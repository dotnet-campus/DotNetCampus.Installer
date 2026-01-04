using Microsoft.DotNet.Archive;

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [Obsolete("请使用异步的方法")]
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

        CompressionUtility.Compress(directoryArchiveProxyInputStream, outputFileStream, new NoneProgressReport());

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
    [Obsolete("请使用异步的方法")]
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
    [Obsolete("请使用异步的方法")]
    public static void Decompress(Stream archiveFileStream, DirectoryInfo outputFolder, IProgress<ProgressReport>? progress = null)
    {
        using var directoryArchiveProxyOutputStream = new DirectoryArchiveProxyOutputStream(outputFolder);

        progress ??= new Progress<ProgressReport>();

        // 解压缩 130MB 只需 5 秒
        var stopwatch = Stopwatch.StartNew();
        CompressionUtility.Decompress(archiveFileStream, directoryArchiveProxyOutputStream, progress);
        Console.WriteLine($"Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");
    }

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

    /// <summary>
    /// 压缩文件夹为存档文件
    /// </summary>
    /// <param name="inputFileList"></param>
    /// <param name="outputFileInfo"></param>
    /// <param name="workingDirectoryInfo"></param>
    /// <returns></returns>
    public static async Task CompressAsync(IReadOnlyList<DirectoryArchiveFileInfo> inputFileList, FileInfo outputFileInfo,
        DirectoryInfo workingDirectoryInfo)
    {
        await using var outputFileStream = new FileStream(outputFileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        await CompressAsync(inputFileList, outputFileStream, workingDirectoryInfo);
    }

    /// <summary>
    /// 压缩文件夹为存档文件
    /// </summary>
    /// <param name="inputFileList"></param>
    /// <param name="outputStream"></param>
    /// <param name="workingDirectoryInfo"></param>
    /// <returns></returns>
    // Header 部分
    // CompressHeader 校验
    // FileBlock 压缩后的内容长度: Int64
    // 压缩后的 FileBlock 内容
    // 压缩后的各个文件内容
    //
    // FileBlock 部分结构：
    // - FileBlockCount: Int32
    // - FileBlock 列表
    // FileBlock:
    // - FileBlockLength: Int32
    // - RelativePathLength: Int32
    // - RelativePath: String
    // - FileContentOffset: Int64
    // - FileLength 压缩后的文件长度: Int64
    // 按照 FileBlock 顺序存放各个文件
    //
    // 压缩实现逻辑：
    // 1. 制作 FileBlock 列表，且将其压缩，写入到输出流中
    // 2. 并行地将各个文件压缩到临时文件中
    // 3. 按照 FileBlock 列表的顺序，将各个文件内容写入到输出流中
    public static async Task CompressAsync(IReadOnlyList<DirectoryArchiveFileInfo> inputFileList, Stream outputStream,
        DirectoryInfo workingDirectoryInfo)
    {
        workingDirectoryInfo.Create();

        CompressProgressFile[] progressFileList = new CompressProgressFile[inputFileList.Count];

        await Parallel.ForAsync(0, inputFileList.Count, async (index, _) =>
        {
            var info = inputFileList[index];

            var file = Path.Join(workingDirectoryInfo.FullName, info.RelativePath);
            var fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096,
                // 设置 DeleteOnClose 这样文件在使用完成后会被自动删除
                FileOptions.DeleteOnClose | FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var sourceFileStream = info.FileInfo.OpenRead();
            progressFileList[index] = new CompressProgressFile(info, fileStream);

            CompressionUtility.Compress(sourceFileStream, fileStream, new NoneProgressReport());
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
        outputStream.Write(CompressHeader);
        // 再写入 FileBlock 的长度
        var writer = new StackallocStreamWriter(outputStream);
        writer.WriteInt64(fileBlockMemoryStream.Length);
        // 写入文件块信息
        fileBlockMemoryStream.Seek(0, SeekOrigin.Begin);
        await fileBlockMemoryStream.CopyToAsync(outputStream);

        // 写入各个文件内容
        foreach (CompressProgressFile compressProgressFile in progressFileList)
        {
            compressProgressFile.CompressFileStream.Seek(0, SeekOrigin.Begin);
            await compressProgressFile.CompressFileStream.CopyToAsync(outputStream);
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
        // 写入文件块数量
        var writer = new StackallocStreamWriter(fileBlockMemoryStream);
        writer.WriteInt32(progressFileList.Length);

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
        CompressionUtility.Compress(fileBlockMemoryStream, fileBlockOutputStream, new NoneProgressReport());
        return fileBlockOutputStream;

        static void WriteFileBlock(Stream stream, in FileBlock fileBlock)
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

    /// <summary>
    /// 解压缩存档文件到文件夹
    /// </summary>
    /// <param name="archiveFileInfo"></param>
    /// <param name="outputFolder"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task DecompressAsync(FileInfo archiveFileInfo, DirectoryInfo outputFolder, DirectoryArchiveDecompressProgress? progress = null)
    {
        await using var archiveFileStream = archiveFileInfo.OpenRead();

        progress ??= new DirectoryArchiveDecompressProgress(shouldIgnore: true);
        await DecompressAsync(archiveFileStream, outputFolder, progress);
    }

    public static async Task DecompressAsync(Stream archiveFileStream, DirectoryInfo outputFolder, DirectoryArchiveDecompressProgress progress)
    {
        var header = await DecompressDirectoryArchiveHeaderAsync(archiveFileStream);
        var fileBlockList = header.FileBlockList;
        var contentPosition = header.ContentPosition;

        progress.Start(fileBlockList.Count);

        for (var i = 0; i < fileBlockList.Count; i++)
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

            await using var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var contentFileStartPosition = contentPosition + fileBlock.FileContentOffset;
            await using var fileCompressedStream = new SliceStream(archiveFileStream, contentFileStartPosition,
                fileBlock.FileLength, leaveOpen: true);
            CompressionUtility.Decompress(fileCompressedStream, outputFileStream, progress.UpdateCurrentDecompress(outputFilePath));

            progress.SetCurrentDecompressFinish();
        }

        progress.Finish();
    }

    private static async ValueTask<DecompressDirectoryArchiveHeader> DecompressDirectoryArchiveHeaderAsync(Stream archiveFileStream)
    {
        long startPosition = archiveFileStream.Position;

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
        CompressionUtility.Decompress(fileBlockInputStream, fileBlockStream, new NoneProgressReport());
        fileBlockStream.Seek(0, SeekOrigin.Begin);

        FileBlock[] fileBlockList = ParseFileBlockList(fileBlockStream);

        // 内容的开始位置就是： 去掉头部 + 文件块长度字段 + 文件块内容
        var contentPosition = startPosition
                              + headerLength
                              + sizeof(long) // fileBlockLengthField
                              + fileBlockLength;
        // 当前刚好就读取到内容位置
        Debug.Assert(contentPosition == archiveFileStream.Position);

        return new DecompressDirectoryArchiveHeader()
        {
            ArchiveStream = archiveFileStream,
            StartPosition = startPosition,
            ContentPosition = contentPosition,
            FileBlockLength = fileBlockLength,
            FileBlockList = fileBlockList,
        };
    }

    /// <summary>
    /// 解压缩文件块列表
    /// </summary>
    /// <returns></returns>
    /// 传入的一般都是内存流，也就没有异步的必要
    private static FileBlock[] ParseFileBlockList(MemoryStream fileBlockStream)
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

    readonly record struct DecompressDirectoryArchiveHeader
    {
        public required Stream ArchiveStream { get; init; }

        public required long StartPosition { get; init; }

        public required long FileBlockLength { get; init; }
        public required IReadOnlyList<FileBlock> FileBlockList { get; init; }
        public required long ContentPosition { get; init; }
    }

    /// <summary>
    /// 打开读取存档文件
    /// </summary>
    /// <param name="archiveFileInfo"></param>
    /// <returns></returns>
    public static async Task<ReadOnlyDirectoryArchive> OpenReadAsync(FileInfo archiveFileInfo)
    {
        var archiveStream = archiveFileInfo.OpenRead();
        // 不能释放 archiveStream 对象，应该被 ReadOnlyDirectoryArchive 所释放
        return await OpenReadAsync(archiveStream);
    }

    /// <summary>
    /// 打开读取存档文件
    /// </summary>
    /// <param name="archiveStream"></param>
    /// <returns></returns>
    public static async Task<ReadOnlyDirectoryArchive> OpenReadAsync(Stream archiveStream)
    {
        var header = await DecompressDirectoryArchiveHeaderAsync(archiveStream);

        var directoryArchiveEntryFiles = new IDirectoryArchiveEntryFile[header.FileBlockList.Count];
        for (int i = 0; i < header.FileBlockList.Count; i++)
        {
            directoryArchiveEntryFiles[i] = new DirectoryArchiveEntryFile()
            {
                Header = header,
                FileBlockIndex = i
            };
        }

        return new ReadOnlyDirectoryArchive(archiveStream)
        {
            EntryFileList = directoryArchiveEntryFiles
        };
    }

    class DirectoryArchiveEntryFile : IDirectoryArchiveEntryFile
    {
        public required DecompressDirectoryArchiveHeader Header { get; init; }
        public required int FileBlockIndex { get; init; }

        private FileBlock FileBlock => Header.FileBlockList[FileBlockIndex];

        public string RelativePath => FileBlock.RelativePath;

        public async Task CopyToAsync(Stream destinationStream, IProgress<ProgressReport>? progress = null)
        {
            var fileBlock = FileBlock;
            var archiveFileStream = Header.ArchiveStream;
            var contentPosition = Header.ContentPosition;

            var contentFileStartPosition = contentPosition + fileBlock.FileContentOffset;

            await using var fileCompressedStream = new SliceStream(archiveFileStream, contentFileStartPosition,
                fileBlock.FileLength, leaveOpen: true);
            progress ??= new NoneProgressReport();

            CompressionUtility.Decompress(fileCompressedStream, destinationStream, progress);
        }

        public async Task SaveToFileAsync(FileInfo outputFile, IProgress<ProgressReport>? progress = null)
        {
            await using var outputFileStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
            await CopyToAsync(outputFileStream, progress);
        }
    }
}

file readonly record struct StackallocStreamWriter(Stream Stream)
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

file readonly record struct StackallocStreamReader(Stream Stream)
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