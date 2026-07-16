using System.Diagnostics;
using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms.DirectoryArchiveProviders;

/// <summary>
/// 放在 PE 文件的 Overlay 部分的安装器内容信息
/// </summary>
public class PEOverlayDirectoryArchiveProvider : IDirectoryArchiveProvider
{
    public async Task<IDirectoryArchive> GetDirectoryArchiveAsync(InstallerLogger logger)
    {
        if (_cacheDirectoryArchive is not null)
        {
            return _cacheDirectoryArchive;
        }

        var reader = new PEOverlayContentReader();
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath != null);

        var installerContent = await reader.ReadOverlayInstallerContent(new FileInfo(processPath), logger);

        if (installerContent is null)
        {
            throw new InvalidOperationException($"当前 PE 文件未包含 PE Overlay 内容，无法读取到安装内容");
        }

        var overlayInstallerContentStream = installerContent.Value.ContentStream;

        // 由于打开的是 PE 资源，可以持续保持 Stream 引用，传入的 leaveOpen 为 true 值。这样即使被外面调用释放了也没有影响
        IDirectoryArchive readOnlyDirectoryArchive =
            await DirectoryArchive.OpenReadAsync(overlayInstallerContentStream, leaveOpen: true);

        _cacheDirectoryArchive = readOnlyDirectoryArchive;

        return readOnlyDirectoryArchive;
    }

    private IDirectoryArchive? _cacheDirectoryArchive;
}