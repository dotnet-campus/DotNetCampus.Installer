using DotNetCampus.Installer.Lib.Hosts.Contexts;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器的安装上下文配置信息
/// </summary>
public record StandardInstallContext
{
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
        get => _displayProductName ?? ProductFamily;
        init => _displayProductName  = value;
    }

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
    private readonly string? _displayProductName;

    /// <summary>
    /// 安装内容的资源信息
    /// </summary>
    public required AssemblyManifestResourceInfo? ContentResourceAssetsInfo { get; init; }

    /// <summary>
    /// 启动图的资源信息。可为空，为空表示不使用启动图
    /// </summary>
    public required AssemblyManifestResourceInfo? SplashScreenResourceAssetsInfo { get; init; }

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
}