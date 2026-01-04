using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using Microsoft.DotNet.Archive;

using System.Buffers;
using System.Runtime.InteropServices;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public class DirectoryArchiveTest
{
    [TestMethod]
    public async Task TestMethod2()
    {
        // 先尝试制造垃圾
        var testFolder = @"F:\Temp\DirectoryArchive2";

        if (!Directory.Exists(testFolder))
        {
            return;
        }

        if (!Directory.EnumerateFiles(testFolder).Any())
        {
            var fileCount = 200;
            var fileLength = 1024 * 1024 * 10; // 10 MB

            var buffer = new byte[1024 * 1024];
            var random = Random.Shared;

            for (int fileIndex = 0; fileIndex < fileCount; fileIndex++)
            {
                var archiveFile = Path.Join(testFolder, $"Test{fileIndex}.archive");
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

        var outputFileInfo = new FileInfo("1.assets");
        var workingFolder = Directory.CreateDirectory(@"C:\lindexi\Work\DirectoryArchiveWork");

        await DirectoryArchive.CompressAsync(new DirectoryInfo(testFolder), outputFileInfo, workingFolder);

        var outputFolder = Path.Join(AppContext.BaseDirectory, "Output");
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }

        await DirectoryArchive.DecompressAsync(outputFileInfo, new DirectoryInfo(outputFolder));

        await AssetsDirectoryEqual(testFolder, outputFolder);
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
           await AssertHelper.AssertStreamEqualAsync(a,b);
        }
    }

    [TestMethod]
    public async Task TestMethod1()
    {
        // 先尝试制造垃圾
        var testFolder = @"G:\Temp\DirectoryArchive";
        if (!Directory.Exists(testFolder))
        {
            return;
        }

        var archiveFile = Path.Join(testFolder, "Test.archive");
        if (!File.Exists(archiveFile))
        {
            using (var fileStream = File.Create(archiveFile))
            {
                var buffer = new byte[1024 * 1024];
                var random = Random.Shared;

                for (int i = 0; i < 1024 * 4; i++)
                {
                    random.NextBytes(buffer);

                    await fileStream.WriteAsync(buffer);
                }
            }
        }

        var outputFileInfo = new FileInfo("1.assets");
        if (!outputFileInfo.Exists)
        {
            DirectoryArchive.Compress(new DirectoryInfo(testFolder), outputFileInfo);
        }

        var outputFolder = Path.Join(AppContext.BaseDirectory, "Output");
        DirectoryArchive.Decompress(outputFileInfo, new DirectoryInfo(outputFolder));

        var testFile = Path.Join(outputFolder, "Test.archive");
        await using var a = File.OpenRead(archiveFile);
        await using var b = File.OpenRead(testFile);

        await AssertHelper.AssertStreamEqualAsync(a, b);
    }
}