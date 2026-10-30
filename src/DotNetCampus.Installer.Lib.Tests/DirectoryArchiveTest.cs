using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using Microsoft.DotNet.Archive;

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public class DirectoryArchiveTest
{
    [TestMethod]
    public async Task TestRelativePathCompression1()
    {
        var testFolder = Path.Join(AppContext.BaseDirectory, $"Test_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(testFolder);

        var outputFileInfo = new FileInfo(Path.Join(testFolder, "Output.assets"));

        var workingFolder = Directory.CreateDirectory(Path.Join(testFolder, "Working"));

        var inputFolder = Path.Join(testFolder, $"File");
        var folderCount = 10;
        var fileLength = 1024 * 1024 * 1; // 1 MB
        var fileCount = 10;

        List<FileInfo> testFileInfoList = [];
        for (int folderIndex = 0; folderIndex < folderCount; folderIndex++)
        {
            var subFolder = Path.Join(inputFolder, $"{folderIndex}");
            Directory.CreateDirectory(subFolder);
            var fileInfoList = await CreateTestDataFileList(new DirectoryInfo(subFolder), fileCount, fileLength);
            testFileInfoList.AddRange(fileInfoList);
        }

        var inputFileList = new List<DirectoryArchiveFileInfo>(testFileInfoList.Count);
        foreach (var fileInfo in testFileInfoList)
        {
            var relativePath = Path.GetRelativePath(inputFolder, fileInfo.FullName);

            inputFileList.Add(new DirectoryArchiveFileInfo()
            {
                RelativePath = relativePath,
                CompressMode = CompressMode.NoCompression,
                FileInfo = fileInfo,
            });
        }

        await DirectoryArchive.CompressAsync(inputFileList, outputFileInfo, workingFolder);

        IDirectoryArchive directoryArchive = await DirectoryArchive.OpenReadAsync(outputFileInfo);
        Assert.HasCount(folderCount * fileCount, directoryArchive.EntryFileList);

        var hashSet = directoryArchive.EntryFileList.Select(t => t.RelativePath.RelativePath).ToImmutableHashSet();
        for (int folderIndex = 0; folderIndex < folderCount; folderIndex++)
        {
            for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
            {
                var relativePath = $@"{folderIndex}\Test{fileIndex}.archive";
                Assert.IsTrue(hashSet.Contains(relativePath));
            }
        }
    }

    [TestMethod]
    public async Task TestNoCompressionMethod1()
    {
        var testFolder = Path.Join(AppContext.BaseDirectory, $"Test_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(testFolder);
        var inputFolder = Path.Join(testFolder, $"File");
        var testInputFolder = Directory.CreateDirectory(inputFolder);

        var fileCount = 10;
        var fileLength = 1024 * 1024 * 10; // 10 MB

        var testFileInfoList = await CreateTestDataFileList(testInputFolder, fileCount, fileLength);

        var outputFileInfo = new FileInfo(Path.Join(testFolder, "Output.assets"));

        var workingFolder = Directory.CreateDirectory(Path.Join(testFolder, "Working"));

        var inputFileList = new List<DirectoryArchiveFileInfo>(testFileInfoList.Count);
        var beNoCompression = false;
        foreach (var fileInfo in testFileInfoList)
        {
            inputFileList.Add(new DirectoryArchiveFileInfo()
            {
                RelativePath = fileInfo.Name,
                CompressMode = beNoCompression ? CompressMode.LZMA : CompressMode.NoCompression,
                FileInfo = fileInfo,
            });

            beNoCompression = !beNoCompression;
        }

        await DirectoryArchive.CompressAsync(inputFileList, outputFileInfo, workingFolder);

        IDirectoryArchive directoryArchive = await DirectoryArchive.OpenReadAsync(outputFileInfo);
        Assert.HasCount(fileCount, directoryArchive.EntryFileList);

        var outputFolder = Path.Join(testFolder, "Output");

        await directoryArchive.DecompressAsync(new DirectoryInfo(outputFolder));

        await AssetsDirectoryEqual(inputFolder, outputFolder);

        // 测试另一个解压缩方式
        var outputFolder2 = Path.Join(testFolder, "Output2");
        await DirectoryArchive.DecompressAsync(outputFileInfo, new DirectoryInfo(outputFolder2));
        await AssetsDirectoryEqual(inputFolder, outputFolder2);
    }

    private async Task<List<FileInfo>> CreateTestDataFileList(DirectoryInfo testInputFolder, int fileCount, int fileLength)
    {
        var buffer = new byte[1024 * 1024];
        var random = Random.Shared;

        var fileInfoList = new List<FileInfo>(fileCount);

        for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
        {
            var archiveFile = Path.Join(testInputFolder.FullName, $"Test{fileIndex}.archive");
            if (!File.Exists(archiveFile))
            {
                using (var fileStream = File.Create(archiveFile))
                {
                    var currentFileLength = random.Next(fileLength);

                    for (int i = 0; i < currentFileLength; i += buffer.Length)
                    {
                        random.NextBytes(buffer);
                        var writeCount = Math.Min(buffer.Length, currentFileLength - i);
                        await fileStream.WriteAsync(buffer.AsMemory(0, writeCount));
                    }
                }
            }

            fileInfoList.Add(new FileInfo(archiveFile));
        }

        return fileInfoList;
    }

    [TestMethod]
    public async Task TestMethod3()
    {
        // 先尝试制造垃圾
        var testFolder = Path.Join(AppContext.BaseDirectory, $"Test_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(testFolder);
        var testInputFolder = Path.Join(testFolder, $"File");
        Directory.CreateDirectory(testInputFolder);

        var fileCount = 10;
        var fileLength = 1024 * 1024 * 10; // 10 MB

        var buffer = new byte[1024 * 1024];
        var random = Random.Shared;

        for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
        {
            var archiveFile = Path.Join(testInputFolder, $"Test{fileIndex}.archive");
            if (!File.Exists(archiveFile))
            {
                using (var fileStream = File.Create(archiveFile))
                {
                    var currentFileLength = random.Next(fileLength);

                    for (int i = 0; i < currentFileLength; i += buffer.Length)
                    {
                        random.NextBytes(buffer);
                        var writeCount = Math.Min(buffer.Length, currentFileLength - i);
                        await fileStream.WriteAsync(buffer.AsMemory(0, writeCount));
                    }
                }
            }
        }

        var outputFileInfo = new FileInfo(Path.Join(testFolder, "Output.assets"));

        var workingFolder = Directory.CreateDirectory(Path.Join(testFolder, "Working"));

        await DirectoryArchive.CompressAsync(new DirectoryInfo(testInputFolder), outputFileInfo, workingFolder);

        IDirectoryArchive directoryArchive = await DirectoryArchive.OpenReadAsync(outputFileInfo);
        Assert.HasCount(fileCount, directoryArchive.EntryFileList);

        var outputFolder = Path.Join(testFolder, "Output");

        var logStringBuilder = new StringBuilder();

        var progress = new DirectoryArchiveDecompressProgress();
        progress.Updated += (_, _) =>
        {
            if (progress.IsFinished)
            {
                logStringBuilder.AppendLine($"{progress.TotalProgressPercentage:0.00}");
            }
            else
            {
                logStringBuilder.AppendLine($"[{progress.CurrentDecompressedProgressPercentage:0.00}][{progress.TotalProgressPercentage:0.00}] {progress.CurrentDecompressedPath}");
            }
        };

        await directoryArchive.DecompressAsync(new DirectoryInfo(outputFolder), progress);

        await AssetsDirectoryEqual(testInputFolder, outputFolder);

        var log = logStringBuilder.ToString();
        Assert.IsNotEmpty(log);
    }

    [TestMethod]
    public async Task TestMethod2()
    {
        // 先尝试制造垃圾
        var testFolder = Path.Join(AppContext.BaseDirectory, $"Test_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(testFolder);
        var testInputFolder = Path.Join(testFolder, $"File");
        Directory.CreateDirectory(testInputFolder);

        if (!Directory.EnumerateFiles(testInputFolder).Any())
        {
            var fileCount = 200;
            var fileLength = 1024 * 1024 * 10; // 10 MB

            var buffer = new byte[1024 * 1024];
            var random = Random.Shared;

            for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
            {
                var archiveFile = Path.Join(testInputFolder, $"Test{fileIndex}.archive");
                if (!File.Exists(archiveFile))
                {
                    using (var fileStream = File.Create(archiveFile))
                    {
                        for (int i = 0; i < fileLength / buffer.Length; i++)
                        {
                            random.NextBytes(buffer);
                            await fileStream.WriteAsync(buffer);
                        }
                    }
                }
            }
        }

        var outputFileInfo = new FileInfo(Path.Join(testFolder, "Output.assets"));
        var workingFolder = Directory.CreateDirectory(Path.Join(testFolder, "Working"));

        await DirectoryArchive.CompressAsync(new DirectoryInfo(testInputFolder), outputFileInfo, workingFolder);

        var outputFolder = Path.Join(testFolder, "Output");

        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }

        var logStringBuilder = new StringBuilder();

        var progress = new DirectoryArchiveDecompressProgress();
        progress.Updated += (_, _) =>
        {
            if (progress.IsFinished)
            {
                logStringBuilder.AppendLine($"{progress.TotalProgressPercentage:0.00}");
            }
            else
            {
                logStringBuilder.AppendLine($"[{progress.CurrentDecompressedProgressPercentage:0.00}][{progress.TotalProgressPercentage:0.00}] {progress.CurrentDecompressedPath}");
            }
        };

        await DirectoryArchive.DecompressAsync(outputFileInfo, new DirectoryInfo(outputFolder), progress);

        await AssetsDirectoryEqual(testInputFolder, outputFolder);

        var log = logStringBuilder.ToString();
        Assert.IsNotEmpty(log);
    }

    private async Task AssetsDirectoryEqual(string expectedFolder, string outputFolder)
    {
        var expectedFileSet = Directory
            .EnumerateFiles(expectedFolder, "*", SearchOption.AllDirectories)
            .ToHashSet();
        var outputFileArray = Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories);

        Assert.HasCount(expectedFileSet.Count, outputFileArray);

        foreach (var outputFile in outputFileArray)
        {
            var relativePath = Path.GetRelativePath(outputFolder, outputFile);
            var expectedFile = Path.Join(expectedFolder, relativePath);
            Assert.IsTrue(expectedFileSet.Remove(expectedFile), $"缺少文件 {relativePath}");

            await using var a = File.OpenRead(expectedFile);
            await using var b = File.OpenRead(outputFile);
            await AssertHelper.AssertStreamEqualAsync(a, b);
        }
    }

    //[TestMethod]
    //public async Task TestMethod1()
    //{
    //    // 先尝试制造垃圾
    //    var testFolder = @"G:\Temp\DirectoryArchive";
    //    if (!Directory.Exists(testFolder))
    //    {
    //        return;
    //    }

    //    var archiveFile = Path.Join(testFolder, "Test.archive");
    //    if (!File.Exists(archiveFile))
    //    {
    //        using (var fileStream = File.Create(archiveFile))
    //        {
    //            var buffer = new byte[1024 * 1024];
    //            var random = Random.Shared;

    //            for (int i = 0; i < 1024 * 4; i++)
    //            {
    //                random.NextBytes(buffer);

    //                await fileStream.WriteAsync(buffer);
    //            }
    //        }
    //    }

    //    var outputFileInfo = new FileInfo("1.assets");
    //    if (!outputFileInfo.Exists)
    //    {
    //        DirectoryArchive.Compress(new DirectoryInfo(testFolder), outputFileInfo);
    //    }

    //    var outputFolder = Path.Join(AppContext.BaseDirectory, "Output");
    //    DirectoryArchive.Decompress(outputFileInfo, new DirectoryInfo(outputFolder));

    //    var testFile = Path.Join(outputFolder, "Test.archive");
    //    await using var a = File.OpenRead(archiveFile);
    //    await using var b = File.OpenRead(testFile);

    //    await AssertHelper.AssertStreamEqualAsync(a, b);
    //}
}