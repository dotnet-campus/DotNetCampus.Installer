using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.Configurations;
using DotNetCampus.Installer.Lib.Exceptions;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms.DirectoryArchiveProviders;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装程序的配置
/// </summary>
public class StandardInstallerConfiguration : Configuration
{
    /// <summary>
    /// 应用程序唯一标识符。用于写入到注册表和创建互斥锁等场景
    /// </summary>
    public Guid ProductCodeGuid
    {
        get
        {
            var configurationString = GetString();
            if (configurationString == null || !Guid.TryParse(configurationString, out var productCodeGuid))
            {
                return Guid.Empty;
            }

            return productCodeGuid;
        }
        set => SetValue(value.ToString("D"));
    }

    /// <summary>
    /// 产品名称，如 VisualStudio.exe 。其大小关系为：
    ///   公司名 -> 品牌名(产品族名称) -> 产品名
    /// </summary>
    public string ProductName
    {
        get => GetString();
        set => SetValue(value);
    }

    /// <summary>
    /// 软件内部对用户显示的产品名称
    /// </summary>
    public string DisplayProductName
    {
        get => GetString() ?? ProductName;
        set => SetValue(value);
    }

    /// <summary>
    /// 产品族名称，如 Microsoft。其大小关系为：
    ///   公司名 -> 品牌名(产品族名称) -> 产品名
    /// </summary>
    public string ProductFamily
    {
        get => GetString();
        set => SetValue(value);
    }

    /// <summary>
    /// 对用户展示的产品族名称
    /// </summary>
    /// 如 ProductFamily 叫 dotnet campus，则 DisplayProductFamily 可以叫 DotNet 职业技术学苑
    public string DisplayProductFamily
    {
        get => GetString() ?? ProductFamily;
        set => SetValue(value);
    }

    /// <summary>
    /// 应用版本号
    /// </summary>
    public string AppVersion
    {
        get => GetString() ?? "1.0.0.0";
        set => SetValue(value);
    }

    /// <summary>
    /// 启动器相对于安装的路径
    /// </summary>
    /// <returns>
    /// 如无启动器无入口，则保持空
    /// </returns>
    public string? LauncherExeRelativePath
    {
        get => GetString();
        set => SetValue(value);
    }


    /// <summary>
    /// 控制面板卸载器显示的图标。相对于安装路径的卸载图标路径
    /// </summary>
    public string? UninstallDisplayIconRelativePath
    {
        get => GetString();
        set => SetValue(value);
    }

    /// <summary>
    /// 控制面板卸载器显示的名称。默认使用 <see cref="DisplayProductName"/>
    /// </summary>
    public string UninstallDisplayName
    {
        get => GetString() ?? DisplayProductName;
        set => SetValue(value);
    }

    /// <summary>
    /// 控制面板卸载器显示的版本号。默认使用 <see cref="AppVersion"/>
    /// </summary>
    public string UninstallDisplayVersion
    {
        get => GetString() ?? AppVersion;
        set => SetValue(value);
    }

    /// <summary>
    /// 控制面板卸载器显示的软件大小。为空将在安装之后自动计算。可以手动制定大小，单位为 KB
    /// </summary>
    public int? UninstallEstimatedSize
    {
        get => GetInt32();
        set => SetValue(value);
    }

    /// <summary>
    /// 控制面板卸载器显示的发布者。默认使用 <see cref="DisplayProductFamily"/>
    /// </summary>
    public string UninstallDisplayPublisher
    {
        get => GetString() ?? DisplayProductFamily;
        set => SetValue(value);
    }

    /// <summary>
    /// 卸载器相对于安装路径的路径
    /// </summary>
    public string? UninstallerRelativePath
    {
        get => GetString();
        set => SetValue(value);
    }

    /// <summary>
    /// 根据安装包配置创建标准安装上下文。
    /// </summary>
    /// <param name="directoryArchive">安装包的目录归档。</param>
    /// <param name="directoryInfo">安装过程使用的工作目录。</param>
    /// <returns>填充完成的标准安装上下文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="directoryArchive"/> 或 <paramref name="directoryInfo"/> 为空。</exception>
    /// <exception cref="InstallerConfigurationException">安装包缺少必填配置或必填配置无效。</exception>
    public StandardInstallContext CreateInstallContext(IDirectoryArchive directoryArchive, DirectoryInfo directoryInfo)
    {
        ArgumentNullException.ThrowIfNull(directoryArchive);
        ArgumentNullException.ThrowIfNull(directoryInfo);

        var productCodeGuid = ProductCodeGuid;
        if (productCodeGuid == Guid.Empty)
        {
            throw new InstallerConfigurationException($"安装包配置 {nameof(ProductCodeGuid)} 不能为空，且必须是有效的 GUID。");
        }

        var productName = ProductName;
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new InstallerConfigurationException($"安装包配置 {nameof(ProductName)} 不能为空。");
        }

        var productFamily = ProductFamily;
        if (string.IsNullOrWhiteSpace(productFamily))
        {
            throw new InstallerConfigurationException($"安装包配置 {nameof(ProductFamily)} 不能为空。");
        }

        return new StandardInstallContext
        {
            ProductCodeGuid = productCodeGuid,
            ProductName = productName,
            DisplayProductName = DisplayProductName,
            ProductFamily = productFamily,
            DisplayProductFamily = DisplayProductFamily,
            AppVersion = AppVersion,
            WorkingFolder = directoryInfo,
            ContentResourceAssetsInfo = null,
            SplashScreenResourceAssetsInfo = null,
            LauncherExeRelativePath = LauncherExeRelativePath,
            UninstallDisplayIconRelativePath = UninstallDisplayIconRelativePath,
            UninstallDisplayName = UninstallDisplayName,
            UninstallDisplayVersion = UninstallDisplayVersion,
            UninstallEstimatedSize = UninstallEstimatedSize,
            UninstallDisplayPublisher = UninstallDisplayPublisher,
            UninstallerRelativePath = UninstallerRelativePath,
            DirectoryArchiveProvider = new ManualDirectoryArchiveProvider(directoryArchive),
        };
    }
}
