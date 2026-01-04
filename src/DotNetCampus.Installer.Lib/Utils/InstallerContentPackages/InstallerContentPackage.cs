using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.Installer.Lib.Utils.InstallerArrayPools;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

/// <summary>
/// 安装包的内容包
/// </summary>
public class InstallerContentPackage : IDisposable, IAsyncDisposable
{
    public static async Task<InstallerContentPackage> FromStream(Stream stream, bool leaveOpen = false)
    {
        using var poolBuffer = InstallerArrayPool.Rent<byte>(1024);
        var headerLength = ContentPackageHeader.Length;
        var headerBuffer = poolBuffer.Memory.Slice(0, headerLength);

        var readCount = await stream.ReadAsync(headerBuffer);
        if (readCount != headerLength || !headerBuffer.Span.SequenceEqual(ContentPackageHeader))
        {
            throw new InvalidDataException("内容包头部校验失败，确认该流是有效的安装包内容包");
        }

        // 读取总长度
        var totalHeaderLengthBuffer = poolBuffer.Slice(0, sizeof(int)).Memory;
        await stream.ReadExactlyAsync(totalHeaderLengthBuffer);
        int totalHeaderLength = BitConverter.ToInt32(totalHeaderLengthBuffer.Span);
        var currentStreamOffset = stream.Position;
        var fileContentStartOffset = currentStreamOffset + totalHeaderLength;

        // 读取 FileBlock 列表
        // 先读取文件块数量
        var fileBlockCountBuffer = poolBuffer.Slice(0, sizeof(int)).Memory;
        await stream.ReadExactlyAsync(fileBlockCountBuffer);
        int fileBlockCount = BitConverter.ToInt32(fileBlockCountBuffer.Span);
        var fileInfoList = new List<VirtualContentPackageFileInfo>(fileBlockCount);

        for (int i = 0; i < fileBlockCount; i++)
        {
            // - FileBlockLength: Int32
            // - RelativePathLength: Int32
            // - RelativePath: String
            // - FileContentOffset: Int64
            // - FileLength: Int64
            var fileBlockLengthBuffer = poolBuffer.Slice(0, sizeof(int)).Memory;
            await stream.ReadExactlyAsync(fileBlockLengthBuffer);
            int fileBlockLength = BitConverter.ToInt32(fileBlockLengthBuffer.Span);
            var currentOffset = stream.Position;

            // 读取 RelativePathLength 的内容
            var relativePathLengthBuffer = poolBuffer.Slice(0, sizeof(int)).Memory;
            await stream.ReadExactlyAsync(relativePathLengthBuffer);
            int relativePathLength = BitConverter.ToInt32(relativePathLengthBuffer.Span);

            // 读取 RelativePath 的内容
            using var relativePathBufferContext = InstallerArrayPool.Rent<byte>(relativePathLength);
            var relativePathBuffer = relativePathBufferContext.Memory.Slice(0, relativePathLength);
            await stream.ReadExactlyAsync(relativePathBuffer);

            string relativePath = Encoding.UTF8.GetString(relativePathBuffer.Span);
            // 读取 FileContentOffset 的内容
            var fileContentOffsetBuffer = poolBuffer.Slice(0, sizeof(long)).Memory;
            await stream.ReadExactlyAsync(fileContentOffsetBuffer);
            long fileContentOffset = BitConverter.ToInt64(fileContentOffsetBuffer.Span);
            // 需要转换为在 Stream 里的偏移量
            long fileContentOffsetInStream = fileContentStartOffset + fileContentOffset;
            // 读取 FileLength 的内容

            var fileLengthBuffer = poolBuffer.Slice(0, sizeof(long)).Memory;
            await stream.ReadExactlyAsync(fileLengthBuffer);
            long fileLength = BitConverter.ToInt64(fileLengthBuffer.Span);

            var fileBlock = new VirtualContentPackageFileInfo
            {
                FileBlock = new ContentPackageFileBlock()
                {
                    RelativePathLength = relativePathLength,
                    RelativePath = relativePath,
                    FileContentOffset = fileContentOffsetInStream,
                    FileLength = fileLength,
                },
                Stream = stream
            };
            fileInfoList.Add(fileBlock);

            // 结束的时候，应该等于 currentOffset + fileBlockLength 的值
            var expectedPosition = currentOffset + fileBlockLength;
            if (stream.Position < expectedPosition)
            {
                stream.Position = expectedPosition;
            }
            else if (stream.Position > expectedPosition)
            {
                throw new InvalidOperationException($"框架内部异常，读取的内容超过了实际内容范围");
            }
        }

        if (stream.Position != fileContentStartOffset)
        {
            throw new InvalidOperationException($"框架内部异常，读取完成之后，没有满足定义的内容");
        }

        var installerContentPackage = new InstallerContentPackage(stream, fileInfoList, leaveOpen);
        return installerContentPackage;
    }

    private InstallerContentPackage(Stream originStream, List<VirtualContentPackageFileInfo> fileInfoList, bool leaveOpen)
    {
        OriginStream = originStream;
        _fileInfoList = fileInfoList;
        _leaveOpen = leaveOpen;
    }

    public Stream OriginStream { get; }
    private readonly List<VirtualContentPackageFileInfo> _fileInfoList;
    private readonly bool _leaveOpen;

    public IReadOnlyList<IContentPackageFileInfo> FileList => _fileInfoList;

    public static ReadOnlySpan<byte> ContentPackageHeader => "DotNet Campus Installer Content Package Header 1.0.0 The Length:"u8;

    // Header 部分
    // ContentPackageHeader 校验
    // HeaderLength Int32 长度
    // FileBlockCount Int32 文件块数量
    // FileBlock:
    // - FileBlockLength: Int32
    // - RelativePathLength: Int32
    // - RelativePath: String
    // - FileContentOffset: Int64
    // - FileLength: Int64
    // ContentLength 内容长度
    // 按照 FileBlock 顺序存放各个文件

    private class VirtualContentPackageFileInfo : IContentPackageFileInfo
    {
        public required Stream Stream { get; init; }
        public required ContentPackageFileBlock FileBlock { get; init; }
        //public required int RelativePathLength { get; init; }
        //public required long RelativePathOffset { get; init; }

        ///// <summary>
        ///// 内容的偏移量
        ///// </summary>
        //public required long FileContentOffset { get; init; }

        ///// <summary>
        ///// 文件长度
        ///// </summary>
        //public required long FileLength { get; init; }
        public string RelativePath => FileBlock.RelativePath;
        public long Length => FileBlock.FileLength;

        public Stream OpenRead()
        {
            var sliceStream = new SliceStream(Stream, FileBlock.FileContentOffset, FileBlock.FileLength, leaveOpen: true);
            return sliceStream;
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            OriginStream.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await OriginStream.DisposeAsync();
    }
}