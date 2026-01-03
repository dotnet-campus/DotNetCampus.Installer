using System.Diagnostics;

using Microsoft.DotNet.Archive;

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
}