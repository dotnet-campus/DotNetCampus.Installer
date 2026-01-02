using System.Buffers;

using DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public sealed class InstallerContentPackageTest
{
    [TestMethod]
    public async Task TestReadWrite()
    {
        var testFolder = @"G:\Temp";
        if (!Directory.Exists(testFolder))
        {
            return;
        }

        using var memoryStream = new MemoryStream();

        // 在前面加一点垃圾内容，测试读取时能够跳过这些内容
        for (int i = 0; i < 10240; i++)
        {
            memoryStream.WriteByte(0x00);
        }

        var startOffset = memoryStream.Position;

        var testFileList = Directory.GetFiles(testFolder)
            .Select(t => (ContentPackageFileInfo) new FileInfo(t))
            .ToList();

        // 测试写入
        await InstallerContentPackageWriter.WriteAsync(testFileList, memoryStream);

        // 测试读取
        memoryStream.Seek(startOffset, SeekOrigin.Begin);
        var package = await InstallerContentPackage.FromStream(memoryStream);
        Assert.HasCount(testFileList.Count, package.FileList);

        for (int i = 0; i < testFileList.Count; i++)
        {
            var (originFile, relativePath) = testFileList[i];
            var packageFileInfo = package.FileList[i];
            Assert.AreEqual(relativePath, packageFileInfo.RelativePath);
            Assert.AreEqual(originFile.Length, packageFileInfo.Length);

            await using var originStream = originFile.OpenRead();
            await using var packageStream = packageFileInfo.OpenRead();

            await AssertStreamEqualAsync(originStream, packageStream);
        }
    }

    private async Task AssertStreamEqualAsync(Stream a, Stream b)
    {
        var buffer1 = ArrayPool<byte>.Shared.Rent(10240);
        var buffer2 = ArrayPool<byte>.Shared.Rent(10240);

        try
        {
            while (true)
            {
                var readCount1 = await a.ReadAsync(buffer1.AsMemory());
                var readCount2 = await b.ReadAsync(buffer2.AsMemory());

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
