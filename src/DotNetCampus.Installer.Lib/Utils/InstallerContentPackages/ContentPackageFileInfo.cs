namespace DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

/// <summary>
/// 文件信息
/// </summary>
/// <param name="File"></param>
/// <param name="RelativePath"></param>
/// 这个结构体的作用只是为了能够让文件和准备存放进去的文件相对路径对应起来。正常情况下，对于安装包来说，只需要有文件名即可。但是考虑到扩展，还是允许加上相对路径
public readonly record struct ContentPackageFileInfo(FileInfo File, string RelativePath)
{
    public static implicit operator ContentPackageFileInfo(FileInfo file)
    {
        return new ContentPackageFileInfo(file, file.Name);
    }
}