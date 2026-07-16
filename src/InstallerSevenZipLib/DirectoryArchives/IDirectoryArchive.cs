using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives.Exceptions;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 压缩目录存档
/// </summary>
public interface IDirectoryArchive : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 包含的文件列表
    /// </summary>
    IReadOnlyList<IDirectoryArchiveEntryFile> EntryFileList { get; }

    /// <summary>
    /// 尝试获取指定路径的文件条目
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="entryFile"></param>
    /// <returns></returns>
    bool TryGetEntryFile(DirectoryArchiveEntryRelativePath relativePath, [NotNullWhen(true)]
        out IDirectoryArchiveEntryFile? entryFile)
    {
        entryFile = EntryFileList.FirstOrDefault(t => t.RelativePath.Equals(relativePath));
        return entryFile != null;
    }

    /// <summary>
    /// 获取指定路径的文件条目。如果只是想尝试获取，请使用 <see cref="TryGetEntryFile"/> 进行查找。此方法会在未找到时抛出异常。
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryArchiveEntryFileNotFoundException"></exception>
    IDirectoryArchiveEntryFile GetEntryFile(DirectoryArchiveEntryRelativePath relativePath)
    {
        if (!TryGetEntryFile(relativePath, out var entryFile))
        {
            throw new DirectoryArchiveEntryFileNotFoundException(relativePath)
            {
                EntryFileList = EntryFileList
            };
        }

        return entryFile;
    }

    /// <summary>
    /// 将整个目录存档解压缩到指定的文件夹中
    /// </summary>
    /// <param name="outputFolder"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    async Task DecompressAsync(DirectoryInfo outputFolder, DirectoryArchiveDecompressProgress? progress = null)
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
}