using System.Diagnostics;

using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器的安装上下文配置信息
/// </summary>
public record StandardInstallContext
{
    /// <summary>
    /// 安装包单例所用的互斥锁名称。默认使用 <see cref="ProductCodeGuid"/> 的无连接符字符串形式
    /// </summary>
    public string SingletonMutexName
    {
        get => _singletonMutexName ??= ProductCodeGuid.ToString("N");
        set => _singletonMutexName = value;
    }

    private string? _singletonMutexName;

    /// <summary>
    /// 应用程序唯一标识符。用于写入到注册表和创建互斥锁等场景
    /// </summary>
    public required Guid ProductCodeGuid { get; init; }

    /// <summary>
    /// 产品名称，如 VisualStudio.exe 。其大小关系为：
    ///   公司名 -> 品牌名(产品族名称) -> 产品名
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// 产品族名称，如 Microsoft。其大小关系为：
    ///   公司名 -> 品牌名(产品族名称) -> 产品名
    /// </summary>
    public required string ProductFamily { get; init; }

    /// <summary>
    /// 软件内部对用户显示的产品名称
    /// </summary>
    public string DisplayProductName
    {
        get => _displayProductName ?? ProductName;
        init => _displayProductName = value;
    }
    private readonly string? _displayProductName;

    /// <summary>
    /// 应用版本号
    /// </summary>
    public string AppVersion { get; set; } = "1.0.0.0";

    /// <summary>
    /// 工作路径，一般是临时文件夹
    /// </summary>
    public DirectoryInfo WorkingFolder
    {
        get
        {
            if (_workingFolder is null)
            {
                var tempPath = Path.GetTempPath();
                var workingFolder = Path.Join(tempPath, $"Installer_{Path.GetRandomFileName()}");
                _workingFolder = Directory.CreateDirectory(workingFolder);
            }

            return _workingFolder;
        }
        set => _workingFolder = value;
    }

    private DirectoryInfo? _workingFolder;

    /// <summary>
    /// 安装内容的资源信息
    /// </summary>
    public required AssemblyManifestResourceInfo? ContentResourceAssetsInfo { get; init; }

    /// <summary>
    /// 启动图的资源信息。可为空，为空表示不使用启动图
    /// </summary>
    public required AssemblyManifestResourceInfo? SplashScreenResourceAssetsInfo { get; init; }

    /// <summary>
    /// 启动器相对于 <see cref="MainInstallPath"/> 的路径
    /// </summary>
    /// <returns>
    /// 如无启动器无入口，则保持空
    /// </returns>
    public string? LauncherExeRelativePath
    {
        get;
        set;
    }

    public string? GetLauncherExeFullPath() => GetFullPath(LauncherExeRelativePath);

    /// <summary>
    /// 安装路径
    /// </summary>
    public string InstallRootPath
    {
        get
        {
            if (_installRootPath is null)
            {
                var programFile = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                _installRootPath = Path.Join(programFile, ProductFamily, ProductName);
            }

            return _installRootPath;
        }
        set => _installRootPath = value;
    }

    private string? _installRootPath;

    /// <summary>
    /// 主安装路径，即为 <see cref="InstallRootPath"/> 下的具体安装目录，带上版本号的路径
    /// </summary>
    public string MainInstallPath
    {
        get => _mainInstallPath ??= Path.Join(InstallRootPath, $"{ProductName}_{AppVersion}");
        set => _mainInstallPath = value;
    }
    private string? _mainInstallPath;

    /// <summary>
    /// 控制面板卸载器显示的图标。相对于 <see cref="MainInstallPath"/> 的卸载图标路径
    /// </summary>
    public string? UninstallDisplayIconRelativePath
    {
        get;
        set;
    }

    public string? GetUninstallDisplayIconFullPath() => GetFullPath(UninstallDisplayIconRelativePath);

    /// <summary>
    /// 控制面板卸载器显示的名称。默认使用 <see cref="DisplayProductName"/>
    /// </summary>
    public string UninstallDisplayName
    {
        get => _uninstallDisplayName ??= DisplayProductName;
        set => _uninstallDisplayName = value;
    }
    private string? _uninstallDisplayName;

    /// <summary>
    /// 控制面板卸载器显示的版本号。默认使用 <see cref="AppVersion"/>
    /// </summary>
    public string UninstallDisplayVersion
    {
        get => _uninstallDisplayVersion ??= AppVersion;
        set => _uninstallDisplayVersion = value;
    }
    private string? _uninstallDisplayVersion;

    /// <summary>
    /// 控制面板卸载器显示的软件大小。为空将在安装之后自动计算。可以手动制定大小，单位为 KB
    /// </summary>
    public int? UninstallEstimatedSize
    {
        get;
        set;
    }

    /// <summary>
    /// 控制面板卸载器显示的发布者。默认使用 <see cref="ProductFamily"/>
    /// </summary>
    public string UninstallDisplayPublisher
    {
        get => _uninstallDisplayPublisher ?? ProductFamily;
        set => _uninstallDisplayPublisher = value;
    }
    private string? _uninstallDisplayPublisher;

    /// <summary>
    /// 卸载器相对于 <see cref="MainInstallPath"/> 的路径
    /// </summary>
    public string? UninstallerRelativePath
    {
        get;
        set;
    }

    public string? GetUninstallerFullPath() => GetFullPath(UninstallerRelativePath);

    /// <summary>
    /// 安装器的 UI 界面，用于弹出对话框的时候能够设置窗口
    /// </summary>
    public IntPtr InstallerUIWindowHandler { get; set; }

    private string? GetFullPath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }
        return Path.Join(MainInstallPath, relativePath);
    }

    /// <summary>
    /// 将放在 PE 文件的 Overlay 部分的安装器内容作为目录归档读取出来
    /// </summary>
    /// <returns></returns>
    public async Task<ReadOnlyDirectoryArchive?> GetOverlayDirectoryArchive()
    {
        if (_directoryArchive != null)
        {
            return _directoryArchive;
        }

        var overlayInstallerContentStream = await GetOverlayInstallerContentStream();
        if (overlayInstallerContentStream is null)
        {
            return null;
        }

        ReadOnlyDirectoryArchive readOnlyDirectoryArchive = await DirectoryArchive.OpenReadAsync(overlayInstallerContentStream);
        _directoryArchive = readOnlyDirectoryArchive;
        return _directoryArchive;
    }

    private ReadOnlyDirectoryArchive? _directoryArchive;

    /// <summary>
    /// 获取放在 PE 文件的 Overlay 部分的安装器内容信息
    /// </summary>
    /// <returns></returns>
    public async Task<Stream?> GetOverlayInstallerContentStream()
    {
        var overlayInstallerContentInfo = await GetOverlayInstallerContentInfo();
        return overlayInstallerContentInfo?.ContentStream;
    }

    /// <summary>
    /// 获取放在 PE 文件的 Overlay 部分的安装器内容信息
    /// </summary>
    /// <returns></returns>
    public Task<OverlayInstallerContentInfo?> GetOverlayInstallerContentInfo()
    {
        var reader = new PEOverlayContentReader();
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath != null);
        return reader.ReadOverlayInstallerContent(new FileInfo(processPath));
    }

    /// <summary>
    /// 日志记录器
    /// </summary>
    public InstallerLogger Logger { get; } = new();
}