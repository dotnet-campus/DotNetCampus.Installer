using Microsoft.DotNet.Archive;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 用本地文件夹作为压缩目录存档，用于调试或测试
/// </summary>
public class FakeDirectoryArchive : IDirectoryArchive
{
    /// <summary>
    /// 使用指定文件条目创建本地目录存档
    /// </summary>
    /// <param name="entryFileList">文件条目列表</param>
    public FakeDirectoryArchive(IReadOnlyList<IDirectoryArchiveEntryFile> entryFileList)
    {
        ArgumentNullException.ThrowIfNull(entryFileList);

        EntryFileList = entryFileList;
    }

    /// <summary>
    /// 使用指定本地文件夹创建目录存档
    /// </summary>
    /// <param name="directoryInfo">作为目录存档的本地文件夹</param>
    public FakeDirectoryArchive(DirectoryInfo directoryInfo)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        var fileArray = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        var entryFileList = new IDirectoryArchiveEntryFile[fileArray.Length];

        for (var i = 0; i < fileArray.Length; i++)
        {
            var fileInfo = fileArray[i];
            var relativePath = Path.GetRelativePath(directoryInfo.FullName, fileInfo.FullName);
            entryFileList[i] = new FakeDirectoryArchiveEntryFile(fileInfo, relativePath);
        }

        EntryFileList = entryFileList;
    }

    /// <inheritdoc />
    public IReadOnlyList<IDirectoryArchiveEntryFile> EntryFileList { get; }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed class FakeDirectoryArchiveEntryFile : IDirectoryArchiveEntryFile
    {
        public FakeDirectoryArchiveEntryFile(FileInfo fileInfo, DirectoryArchiveEntryRelativePath relativePath)
        {
            ArgumentNullException.ThrowIfNull(fileInfo);

            _fileInfo = fileInfo;
            RelativePath = relativePath;
        }

        private readonly FileInfo _fileInfo;

        public DirectoryArchiveEntryRelativePath RelativePath { get; }

        public long CompressedFileLength => _fileInfo.Length;

        public long OriginFileLength => _fileInfo.Length;

        public async Task CopyToAsync(Stream destinationStream, IProgress<ProgressReport>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(destinationStream);

            await using var sourceFileStream = _fileInfo.OpenRead();
            await sourceFileStream.CopyToAsync(destinationStream).ConfigureAwait(false);
        }

        public async Task SaveToFileAsync(FileInfo outputFile, IProgress<ProgressReport>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(outputFile);

            outputFile.Directory?.Create();
            await using var outputFileStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write,
                FileShare.None);
            await CopyToAsync(outputFileStream, progress).ConfigureAwait(false);
        }
    }
}