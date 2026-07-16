namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 只读的压缩目录存档
/// </summary>
internal class ReadOnlyDirectoryArchive : IDirectoryArchive, IDisposable, IAsyncDisposable
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