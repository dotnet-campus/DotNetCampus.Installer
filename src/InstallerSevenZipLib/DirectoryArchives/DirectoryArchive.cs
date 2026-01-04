using Microsoft.DotNet.Archive;

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 文件夹存档
/// </summary>
public static partial class DirectoryArchive
{
    /// <summary>
    /// 压缩文件夹为存档文件
    /// </summary>
    /// <param name="inputDirectoryInfo"></param>
    /// <param name="outputFileInfo"></param>
    /// <exception cref="Exception"></exception>
    public static void Compress(DirectoryInfo inputDirectoryInfo, FileInfo outputFileInfo)
    {
        using var outputFileStream = new FileStream(outputFileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        FileInfo[] fileArray = inputDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);

        var headStream = new MemoryStream();
        long totalFileLength = 0;

        for (int i = 0; i < sizeof(long); i++)
        {
            // 预留的内容，用来后续填充长度信息
            headStream.WriteByte(0xFF);
        }

        var streamWriter = new StreamWriter(headStream);
        foreach (var fileInfo in fileArray)
        {
            totalFileLength += fileInfo.Length;
            var relativePath = Path.GetRelativePath(inputDirectoryInfo.FullName, fileInfo.FullName);
            streamWriter.WriteLine($"{relativePath}");
            streamWriter.WriteLine($"Length={fileInfo.Length}");
            streamWriter.WriteLine();
        }
        streamWriter.Flush();
        var headLength = headStream.Position;
        Debug.Assert(headStream.Position == headStream.Length);
        totalFileLength += headLength;

        headStream.Position = 0;
        for (int i = 0; i < sizeof(long); i++)
        {
            headStream.WriteByte((byte) (headLength >> (8 * i)));
        }
        headStream.Position = 0;

        var currentIndex = 0;
        FileStream? currentFileStream = null;

        var directoryArchiveProxyInputStream = new DirectoryArchiveProxyInputStream(headStream, totalFileLength);
        directoryArchiveProxyInputStream.ReadNext += (_, args) =>
        {
            // ReSharper disable AccessToDisposedClosure
            if (currentFileStream is not null && !ReferenceEquals(currentFileStream, args.CurrentInputStream))
            {
                throw new Exception();
            }

            while (currentIndex < fileArray.Length)
            {
                FileInfo fileInfo = fileArray[currentIndex];
                if (fileInfo.Length == 0)
                {
                    // 跳过空文件
                    currentIndex++;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (currentIndex < fileArray.Length)
            {
                args.CurrentInputStream.Dispose();

                FileInfo fileInfo = fileArray[currentIndex];
                var fileStream = fileInfo.OpenRead();
                currentFileStream = fileStream;
                args.UpdateInputStream(fileStream);

                Console.WriteLine($"读取文件中 {currentIndex + 1}/{fileArray.Length} 文件：{fileInfo}");

                currentIndex++;
            }
        };

        var stopwatch = Stopwatch.StartNew();

        CompressionUtility.Compress(directoryArchiveProxyInputStream, outputFileStream, new ConsoleProgressReport());

        stopwatch.Stop();
        Console.WriteLine($"TotalLength={totalFileLength};Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");

        if (currentFileStream is not null)
        {
            currentFileStream.Dispose();
        }
    }

    /// <summary>
    /// 解压缩存档文件到文件夹
    /// </summary>
    /// <param name="archiveFileInfo"></param>
    /// <param name="outputFolder"></param>
    /// <param name="progress"></param>
    public static void Decompress(FileInfo archiveFileInfo, DirectoryInfo outputFolder, IProgress<ProgressReport>? progress = null)
    {
        using var archiveFileStream = archiveFileInfo.OpenRead();
        Decompress(archiveFileStream, outputFolder, progress);
    }

    /// <summary>
    /// 解压缩存档文件到文件夹
    /// </summary>
    /// <param name="archiveFileStream"></param>
    /// <param name="outputFolder"></param>
    /// <param name="progress"></param>
    public static void Decompress(Stream archiveFileStream, DirectoryInfo outputFolder, IProgress<ProgressReport>? progress = null)
    {
        using var directoryArchiveProxyOutputStream = new DirectoryArchiveProxyOutputStream(outputFolder);

        progress ??= new Progress<ProgressReport>();

        // 解压缩 130MB 只需 5 秒
        var stopwatch = Stopwatch.StartNew();
        CompressionUtility.Decompress(archiveFileStream, directoryArchiveProxyOutputStream, progress);
        Console.WriteLine($"Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");
    }

    public static async Task DecompressAsync(FileInfo archiveFileInfo, DirectoryInfo outputFolder)
    {
        await using var archiveFileStream = archiveFileInfo.OpenRead();

        await DecompressAsync(archiveFileStream, outputFolder);
    }

    public static async Task DecompressAsync(Stream archiveFileStream, DirectoryInfo outputFolder)
    {
        var startPosition = archiveFileStream.Position;

        var headerLength = CompressHeader.Length;
        Span<byte> header = stackalloc byte[headerLength];
        archiveFileStream.ReadExactly(header);

        if (!header.SequenceEqual(CompressHeader))
        {
            throw new ArgumentException();
        }

        var reader = new StackallocStreamReader(archiveFileStream);
        var fileBlockLength = reader.ReadInt64();

        await using var fileBlockInputStream = new SliceStream(archiveFileStream, archiveFileStream.Position, fileBlockLength, leaveOpen: true);
        await using var fileBlockStream = new MemoryStream();
        CompressionUtility.Decompress(fileBlockInputStream, fileBlockStream, new ConsoleProgressReport());
        fileBlockStream.Seek(0, SeekOrigin.Begin);

        FileBlock[] fileBlockList = DecompressFileBlockList(fileBlockStream);

        // 内容的开始位置就是： 去掉头部 + 文件块长度字段 + 文件块内容
        var contentPosition = startPosition
                              + headerLength
                              + sizeof(long) // fileBlockLengthField
                              + fileBlockLength;
        // 当前刚好就读取到内容位置
        Debug.Assert(contentPosition == archiveFileStream.Position);

        await using var fileListContentStream = new SliceStream(archiveFileStream, contentPosition,
            archiveFileStream.Length - contentPosition, leaveOpen: true);

        for (var i = 0; i < fileBlockList.Length; i++)
        {
            var fileBlock = fileBlockList[i];
            var outputFilePath = Path.Join(outputFolder.FullName, fileBlock.RelativePath);
            var outputFileDirectory = Path.GetDirectoryName(outputFilePath);
            if (outputFileDirectory is not null)
            {
                Directory.CreateDirectory(outputFileDirectory);
            }
            else
            {
                Debug.Fail($"预期肯定能拿到文件夹");
            }

            await using var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            await using var fileCompressedStream = new SliceStream(fileListContentStream, fileBlock.FileContentOffset,
                fileBlock.FileLength, leaveOpen: true);
            CompressionUtility.Decompress(fileCompressedStream, outputFileStream, new ConsoleProgressReport());
        }
    }

    /// <summary>
    /// 解压缩文件块列表
    /// </summary>
    /// <returns></returns>
    /// 传入的一般都是内存流，也就没有异步的必要
    private static FileBlock[] DecompressFileBlockList(MemoryStream fileBlockStream)
    {
        // 读取文件块数量
        var reader = new StackallocStreamReader(fileBlockStream);
        var fileCount = reader.ReadInt32();
        var fileList = new FileBlock[fileCount];

        for (int i = 0; i < fileCount; i++)
        {
            var fileBlockLength = reader.ReadInt32();

            // 需要在 Read FileBlockLength 才能定下量。否则将会少了 FileBlockLengthField 长度
            var position = fileBlockStream.Position;

            var relativePathLength = reader.ReadInt32();
            var relativePath = reader.ReadString(relativePathLength);
            var fileContentOffset = reader.ReadInt64();
            var fileLength = reader.ReadInt64();

            var expectedPosition = position + fileBlockLength;
            if (fileBlockStream.Position != expectedPosition)
            {
                fileBlockStream.Position = expectedPosition;
            }

            fileList[i] = new FileBlock()
            {
                RelativePathLength = relativePathLength,
                RelativePath = relativePath,
                FileContentOffset = fileContentOffset,
                FileLength = fileLength
            };
        }

        return fileList;
    }

    readonly record struct StackallocStreamReader(Stream Stream)
    {
        public string ReadString(int utf8ByteCount)
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
                Stream.ReadExactly(buffer);
                return Encoding.UTF8.GetString(buffer);
            }
            finally
            {
                if (pool != null)
                {
                    ArrayPool<byte>.Shared.Return(pool);
                }
            }
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            Stream.ReadExactly(buffer);
            return MemoryMarshal.Read<int>(buffer);
        }

        public long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            Stream.ReadExactly(buffer);
            return MemoryMarshal.Read<long>(buffer);
        }
    }
}