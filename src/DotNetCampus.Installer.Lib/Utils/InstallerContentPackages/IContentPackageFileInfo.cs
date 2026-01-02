namespace DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

public interface IContentPackageFileInfo
{
    string RelativePath { get; }
    long Length { get; }
    Stream OpenRead();
}