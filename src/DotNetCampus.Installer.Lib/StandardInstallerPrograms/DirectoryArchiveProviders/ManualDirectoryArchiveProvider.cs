using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms.DirectoryArchiveProviders;

public record ManualDirectoryArchiveProvider(IDirectoryArchive DirectoryArchive) : IDirectoryArchiveProvider
{
    public Task<IDirectoryArchive> GetDirectoryArchiveAsync(InstallerLogger logger)
    {
        return Task.FromResult(DirectoryArchive);
    }
}