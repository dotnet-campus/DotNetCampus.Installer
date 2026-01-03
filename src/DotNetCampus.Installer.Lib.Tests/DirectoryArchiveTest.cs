using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

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
        var testFolder = @"G:\Temp\DirectoryArchive2";

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

        var outputFileInfo = new FileInfo("2.assets");
        var workingFolder = Directory.CreateDirectory(@"G:\Temp\DirectoryArchiveWork");

        await DirectoryArchive.CompressAsync(new DirectoryInfo(testFolder), outputFileInfo, workingFolder);
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

        await AssertStreamEqualAsync(a, b);
    }

    private async Task AssertStreamEqualAsync(Stream a, Stream b)
    {
        const int length = 10240;
        var buffer1 = ArrayPool<byte>.Shared.Rent(length);
        var buffer2 = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            while (true)
            {
                var readCount1 = await a.ReadAsync(buffer1.AsMemory(0, length));
                var readCount2 = await b.ReadAsync(buffer2.AsMemory(0, length));

                Assert.AreEqual(readCount1, readCount2);

                if (readCount1 == 0)
                {
                    break;
                }

                var span1 = buffer1.AsSpan(0, readCount1);
                var span2 = buffer2.AsSpan(0, readCount2);

                Assert.IsTrue(span1.SequenceEqual(span2));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer1);
            ArrayPool<byte>.Shared.Return(buffer2);
        }
    }
}