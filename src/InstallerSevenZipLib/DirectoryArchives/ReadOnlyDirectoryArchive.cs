using System.Diagnostics;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 只读的压缩目录存档
/// </summary>
public class ReadOnlyDirectoryArchive : IDisposable, IAsyncDisposable
{
    internal ReadOnlyDirectoryArchive(Stream archiveStream)
    {
        ArchiveStream = archiveStream;
    }

    internal Stream ArchiveStream { get; }

    /// <summary>
    /// 包含的文件列表
    /// </summary>
    public required IReadOnlyList<IDirectoryArchiveEntryFile> EntryFileList { get; init; }

    /// <summary>
    /// 将整个目录存档解压缩到指定的文件夹中
    /// </summary>
    /// <param name="outputFolder"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
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

    /// <inheritdoc />
    public void Dispose()
    {
        ArchiveStream.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ArchiveStream.DisposeAsync();
    }
}