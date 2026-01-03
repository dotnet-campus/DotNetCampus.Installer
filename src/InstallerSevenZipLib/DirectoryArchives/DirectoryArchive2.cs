using Microsoft.DotNet.Archive;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public static partial class DirectoryArchive
{
    /// <summary>
    /// 压缩文件夹为存档文件
    /// </summary>
    /// <param name="inputDirectoryInfo"></param>
    /// <param name="outputFileInfo"></param>
    /// <param name="workingDirectoryInfo">工作的文件夹，会在这里存放压缩过程中存放的临时文件</param>
    public static async Task CompressAsync(DirectoryInfo inputDirectoryInfo, FileInfo outputFileInfo,
        DirectoryInfo workingDirectoryInfo)
    {
        FileInfo[] fileArray = inputDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);
        var fileList = new List<DirectoryArchiveFileInfo>(fileArray.Length);

        foreach (var fileInfo in fileArray)
        {
            var relativePath = Path.GetRelativePath(inputDirectoryInfo.FullName, fileInfo.FullName);

            fileList.Add(new DirectoryArchiveFileInfo(relativePath, fileInfo));
        }

        await CompressAsync(fileList, outputFileInfo, workingDirectoryInfo);
    }

    public static async Task CompressAsync(IReadOnlyList<DirectoryArchiveFileInfo> inputFileList, FileInfo outputFileInfo,
        DirectoryInfo workingDirectoryInfo)
    {
        await using var outputFileStream = new FileStream(outputFileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        workingDirectoryInfo.Create();

        CompressProgressFile[] progressFileList = new CompressProgressFile[inputFileList.Count];

        await Parallel.ForAsync(0, inputFileList.Count, async (index, _) =>
        {
            var info = inputFileList[index];

            var file = Path.Join(workingDirectoryInfo.FullName, info.RelativePath);
            var fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096,
                // 设置 DeleteOnClose 这样文件在使用完成后会被自动删除
                FileOptions.DeleteOnClose);
            await using var sourceFileStream = info.FileInfo.OpenRead();

            CompressionUtility.Compress(sourceFileStream, fileStream, new ConsoleProgressReport());

            progressFileList[index] = new CompressProgressFile(info, fileStream);
        });

        await using var fileBlockMemoryStream = CompressFileBlockList(progressFileList);

        // 写入头信息
        //var h = "DNCSZLDA"u8;
        //var t = MemoryMarshal.Read<long>(h);
        //var s = MemoryMarshal.CreateSpan(ref t, 1);
        //var t2 = MemoryMarshal.AsBytes(s);

        //for (int i = 0; i < h.Length; i++)
        //{
        //    Assert.AreEqual(h[i], t2[i]);
        //}
        outputFileStream.Write(CompressHeader);
        // 再写入 FileBlock 的长度
        var writer = new StackallocStreamWriter(outputFileStream);
        fileBlockMemoryStream.Seek(0, SeekOrigin.Begin);
        writer.WriteInt64(fileBlockMemoryStream.Length);
        // 写入文件块信息
        await fileBlockMemoryStream.CopyToAsync(outputFileStream);

        // 写入各个文件内容
        foreach (CompressProgressFile compressProgressFile in progressFileList)
        {
            compressProgressFile.CompressFileStream.Seek(0, SeekOrigin.Begin);
            await compressProgressFile.CompressFileStream.CopyToAsync(outputFileStream);
            await compressProgressFile.CompressFileStream.DisposeAsync();
        }
    }

    private static ReadOnlySpan<byte> CompressHeader
        // 这是 DotNetCampus.InstallerSevenZipLib.DirectoryArchives 的缩写，刚好是一个 long 的长度
        => "DNCSZLDA"u8;

    /// <summary>
    /// 获取压缩后的文件块列表
    /// </summary>
    /// <returns></returns>
    private static Stream CompressFileBlockList(CompressProgressFile[] progressFileList)
    {
        // 写入文件块信息。这部分内容也可以进行压缩，进一步减少体积
        using var fileBlockMemoryStream = new MemoryStream();
        long currentOffset = 0;
        foreach (CompressProgressFile compressProgressFile in progressFileList)
        {
            var fileLength = compressProgressFile.CompressFileStream.Length;
            var fileBlock = new FileBlock()
            {
                FileLength = fileLength,
                FileContentOffset = currentOffset,
                RelativePath = compressProgressFile.FileInfo.RelativePath
            };
            currentOffset += fileLength;

            WriteFileBlock(fileBlockMemoryStream, in fileBlock);
        }

        fileBlockMemoryStream.Seek(0, SeekOrigin.Begin);

        // 被压缩的文件块信息
        var fileBlockOutputStream = new MemoryStream();
        CompressionUtility.Compress(fileBlockMemoryStream, fileBlockOutputStream, new ConsoleProgressReport());
        return fileBlockOutputStream;

        void WriteFileBlock(Stream stream, in FileBlock fileBlock)
        {
            var writer = new StackallocStreamWriter(stream);

            var fileBlockLength = fileBlock.FileBlockLength;
            writer.WriteInt32(fileBlockLength);
            writer.WriteInt32(fileBlock.RelativePathLength);
            writer.WriteString(fileBlock.RelativePath, fileBlock.RelativePathLength);
            writer.WriteInt64(fileBlock.FileContentOffset);
            writer.WriteInt64(fileBlock.FileLength);
        }
    }

    readonly record struct StackallocStreamWriter(Stream Stream)
    {
        public void WriteString(string value, int utf8ByteCount)
        {
            scoped Span<byte> buffer;
            byte[]? pool = null;
            if (utf8ByteCount < 512)
            {
                buffer = stackalloc byte[utf8ByteCount];
            }
            else
            {
                pool = ArrayPool<byte>.Shared.Rent(utf8ByteCount);
                buffer = pool.AsSpan(0, utf8ByteCount);
            }

            try
            {
                Encoding.UTF8.GetBytes(value, buffer);
                Stream.Write(buffer);
            }
            finally
            {
                if (pool != null)
                {
                    ArrayPool<byte>.Shared.Return(pool);
                }
            }
        }

        public void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            MemoryMarshal.Write(buffer, value);
            Stream.Write(buffer);
        }

        public void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            MemoryMarshal.Write(buffer, value);
            Stream.Write(buffer);
        }
    }

    /// <summary>
    /// 压缩过程的文件信息
    /// </summary>
    /// <param name="FileInfo"></param>
    /// <param name="CompressFileStream">被压缩的文件</param>
    readonly record struct CompressProgressFile(DirectoryArchiveFileInfo FileInfo, FileStream CompressFileStream);

    /// <summary>
    /// 内容包的文件块信息
    /// </summary>
    readonly record struct FileBlock
    {
        /// <summary>
        /// 整个 FileBlock 的长度。不包括此字段的长度
        /// </summary>
        public int FileBlockLength
        {
            get
            {
                // - FileNameLength: Int32
                // - FileName: String
                // - FileContentOffset: Int64
                // - FileLength: Int64
                int length = sizeof(int) // FileNameLength field
                             + RelativePathLength // FileName field
                             + sizeof(long) // FileContentOffset field
                             + sizeof(long); // FileLength field
                return length;
            }
        }

        /// <summary>
        /// 文件名的长度
        /// </summary>
        public int RelativePathLength { get; init; }

        /// <summary>
        /// 文件名
        /// </summary>
        public required string RelativePath
        {
            get => _relativePath;
            [MemberNotNull(nameof(_relativePath))]
            init
            {
                _relativePath = value;
                if (RelativePathLength == 0)
                {
                    RelativePathLength = Encoding.UTF8.GetByteCount(_relativePath);
                }
            }
        }

        private readonly string _relativePath;

        /// <summary>
        /// 内容的偏移量
        /// </summary>
        public required long FileContentOffset { get; init; }

        /// <summary>
        /// 文件长度
        /// </summary>
        public required long FileLength { get; init; }
    }
}

public readonly record struct DirectoryArchiveFileInfo(string RelativePath, FileInfo FileInfo)
{
}