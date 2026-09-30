using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public class DirectoryArchiveTimestampTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow(CompressMode.NoCompression, 0, "content")]
    [DataRow(CompressMode.NoCompression, 1, "content")]
    [DataRow(CompressMode.NoCompression, 2, "content")]
    [DataRow(CompressMode.LZMA, 0, "content")]
    [DataRow(CompressMode.LZMA, 1, "content")]
    [DataRow(CompressMode.LZMA, 2, "content")]
    [DataRow(CompressMode.NoCompression, 0, "")]
    [DataRow(CompressMode.NoCompression, 1, "")]
    [DataRow(CompressMode.NoCompression, 2, "")]
    [DataRow(CompressMode.LZMA, 0, "")]
    [DataRow(CompressMode.LZMA, 1, "")]
    [DataRow(CompressMode.LZMA, 2, "")]
    public async Task WhenExtractingArchiveThenSourceTimestampsAreRestored(CompressMode mode, int entryPoint, string content)
    {
        var folder = CreateTestFolder();
        var source = await CreateSourceAsync(folder, content);
        var expected = ReadTimestamps(source.FullName);
        var archiveFile = new FileInfo(Path.Join(folder.FullName, "package.assets"));
        await DirectoryArchive.CompressAsync(
            [new DirectoryArchiveFileInfo("nested/file.txt", source) { CompressMode = mode }], archiveFile);
        var outputFolder = Directory.CreateDirectory(Path.Join(folder.FullName, "output"));
        var outputFile = new FileInfo(Path.Join(outputFolder.FullName, "nested/file.txt"));
        outputFile.Directory!.Create();
        await File.WriteAllTextAsync(outputFile.FullName, "existing content");

        await ExtractAsync(archiveFile, outputFolder, outputFile, entryPoint);

        Assert.AreEqual(expected, ReadTimestamps(outputFile.FullName));
    }

    [TestMethod]
    public async Task WhenExtractingLocalFolderThenSourceTimestampsAreRestored()
    {
        var folder = CreateTestFolder();
        var sourceFolder = Directory.CreateDirectory(Path.Join(folder.FullName, "source"));
        var source = await CreateSourceAsync(sourceFolder, "content");
        var expected = ReadTimestamps(source.FullName);
        await using IDirectoryArchive archive = new FakeDirectoryArchive(sourceFolder);
        var outputFolder = new DirectoryInfo(Path.Join(folder.FullName, "output"));

        await archive.DecompressAsync(outputFolder);

        Assert.AreEqual(expected, ReadTimestamps(Path.Join(outputFolder.FullName, source.Name)));
    }

    private DirectoryInfo CreateTestFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Creation time restoration is verified on Windows.");
        }

        var folder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"ArchiveTimestamps_{Path.GetRandomFileName()}"));
        TestContext.WriteLine(folder.FullName);
        return folder;
    }

    private static async Task<FileInfo> CreateSourceAsync(DirectoryInfo folder, string content)
    {
        var path = Path.Join(folder.FullName, "file.txt");
        await File.WriteAllTextAsync(path, content);
        File.SetCreationTimeUtc(path, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(path, new DateTime(2021, 6, 7, 8, 9, 10, DateTimeKind.Utc));
        return new FileInfo(path);
    }

    private static (DateTime CreationTimeUtc, DateTime LastWriteTimeUtc) ReadTimestamps(string path)
        => (File.GetCreationTimeUtc(path), File.GetLastWriteTimeUtc(path));

    private static async Task ExtractAsync(FileInfo archiveFile, DirectoryInfo outputFolder, FileInfo outputFile, int entryPoint)
    {
        if (entryPoint == 0)
        {
            await DirectoryArchive.DecompressAsync(archiveFile, outputFolder);
            return;
        }

        await using var archive = await DirectoryArchive.OpenReadAsync(archiveFile);
        if (entryPoint == 1)
        {
            await archive.DecompressAsync(outputFolder);
            return;
        }

        await archive.EntryFileList[0].SaveToFileAsync(outputFile);
    }
}
