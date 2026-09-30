using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms.DirectoryArchiveProviders;

/// <summary>
/// 本地文件夹的目录归档提供器，用于测试和调试
/// </summary>
public class LocalFolderFakeDirectoryArchiveProvider : IDirectoryArchiveProvider
{
    public LocalFolderFakeDirectoryArchiveProvider(DirectoryInfo directoryInfo)
    {
        var fakeDirectoryArchive = new FakeDirectoryArchive(directoryInfo);
        _fakeDirectoryArchive = fakeDirectoryArchive;
    }

    public Task<IDirectoryArchive> GetDirectoryArchiveAsync(InstallerLogger logger)
    {
        return Task.FromResult((IDirectoryArchive) _fakeDirectoryArchive);
    }

    private readonly FakeDirectoryArchive _fakeDirectoryArchive;
}