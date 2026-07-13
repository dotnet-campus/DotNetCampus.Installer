using System;
using dotnetCampus.Configurations;

namespace DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

class LanguageConfiguration() : Configuration("Lang")
{

}

class ClassicInstallerConfiguration() : Configuration("")
{
    /// <summary>
    /// 应用程序唯一标识符。用于写入到注册表和创建互斥锁等场景
    /// </summary>
    public Guid ProductCodeGuid
    {
        get
        {
            var configurationString = GetString();
            if (configurationString == null)
            {
                return Guid.Empty;
            }

            return Guid.Parse(configurationString);
        }
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
}