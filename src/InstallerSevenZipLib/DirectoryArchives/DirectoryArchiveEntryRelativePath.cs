namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 目录存档的项的相对路径
/// </summary>
/// <param name="RelativePath"></param>
/// 仅仅只是为了当上单位，避免直接使用 string 导致的混淆
public readonly record struct DirectoryArchiveEntryRelativePath(string RelativePath)
{
    public static implicit operator string(DirectoryArchiveEntryRelativePath relativePath)=>
        relativePath.RelativePath;

    public static implicit operator DirectoryArchiveEntryRelativePath(string relativePath) =>
        new DirectoryArchiveEntryRelativePath(relativePath);

    public bool StartsWith(string value, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
        return RelativePath.StartsWith(value, stringComparison);
    }
}