using System.Diagnostics;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public class ReadOnlyDirectoryArchive : IDisposable, IAsyncDisposable
{
    internal ReadOnlyDirectoryArchive(Stream archiveStream)
    {
        ArchiveStream = archiveStream;
    }

    internal Stream ArchiveStream { get; }
    public required IReadOnlyList<IDirectoryArchiveEntryFile> EntryFileList { get; init; }

    public async Task DecompressAsync(DirectoryInfo outputFolder, DirectoryArchiveDecompressProgress? progress = null)
    {
        progress ??= new DirectoryArchiveDecompressProgress(shouldIgnore: true);

        progress.Start(EntryFileList.Count);

        foreach (var entryFile in EntryFileList)
        {
            var outputFilePath = Path.Join(outputFolder.FullName, entryFile.RelativePath);
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
            await entryFile.CopyToAsync(outputFileStream, progress.UpdateCurrentDecompress(outputFilePath));

            progress.SetCurrentDecompressFinish();
        }

        progress.Finish();
    }

    public void Dispose()
    {
        ArchiveStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await ArchiveStream.DisposeAsync();
    }
}