namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives.Exceptions;

/// <summary>
/// 无法从目录存档中找到指定的文件条目时抛出的异常
/// </summary>
public class DirectoryArchiveEntryFileNotFoundException : DirectoryArchiveException
{
    internal DirectoryArchiveEntryFileNotFoundException(DirectoryArchiveEntryRelativePath relativePath)
        : base($"在目录存档中未找到指定的文件条目：{relativePath.RelativePath}")
    {
        RelativePath = relativePath;
    }
    /// <summary>
    /// 未找到的条目路径
    /// </summary>
    public string RelativePath { get; }

    public required IReadOnlyList<IDirectoryArchiveEntryFile> EntryFileList { get; init; }
}