using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.SplashScreens;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器流程
/// </summary>
public abstract class StandardInstallerProgram
{
    public abstract StandardInstallContext StandardInstallContext { get; }

    /// <summary>
    /// 检测环境和弹出提示
    /// </summary>
    /// <returns></returns>
    public virtual bool CheckEnvironment()
    {
        return EnvironmentChecker.CheckEnvironmentAndShowMessageBox();
    }

    /// <summary>
    /// 显示欢迎界面
    /// </summary>
    public void ShowSplashScreen()
    {
        var splashScreenInfo = StandardInstallContext.SplashScreenResourceAssetsInfo;
        if (splashScreenInfo is null)
        {
            return;
        }

        var workingFolder = StandardInstallContext.WorkingFolder;
        var resourceInfo = splashScreenInfo.Value;
        using Stream assetsStream = resourceInfo.GetManifestResourceStream();
        var tempFile = Path.Join(workingFolder.FullName, $"{resourceInfo.ManifestResourceName}");
        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
        {
            assetsStream.CopyTo(fileStream);
        }

        var splashScreenFile = new FileInfo(tempFile);
        var splashScreen = new SplashScreen(splashScreenFile, windowTitle: StandardInstallContext.DisplayProductName);
        splashScreen.ShowAsync();
        _splashScreen = splashScreen;
    }

    private SplashScreen? _splashScreen;

    /// <summary>
    /// 关闭欢迎界面
    /// </summary>
    public void CloseSplashScreen() => _splashScreen?.Close();

    /// <summary>
    /// 写注册表
    /// </summary>
    public virtual void WriteRegister()
    {
        WriteInstallRegister();
        WriteUninstallRegister();
    }

    /// <summary>
    /// 写安装注册表
    /// </summary>
    protected virtual void WriteInstallRegister()
    {
        // 注册表安装项路径
        // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node
        var softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", writable: true)!;

        var productFamilyKey =
            softwareKey.CreateSubKey(StandardInstallContext.ProductFamily);
        var productNameKey = productFamilyKey.CreateSubKey(StandardInstallContext.ProductName);

        var launcherExeFullPath = StandardInstallContext.GetLauncherExeFullPath();
        if (!string.IsNullOrEmpty(launcherExeFullPath))
        {
            productNameKey.SetValue("ActualExePath", launcherExeFullPath, RegistryValueKind.String);

            productNameKey.SetValue("ExePath", launcherExeFullPath, RegistryValueKind.String);
        }

        var code = StandardInstallContext.ProductCodeGuid.ToString("B");
        productNameKey.SetValue("code", code, RegistryValueKind.String);

        productNameKey.SetValue("path", StandardInstallContext.InstallRootPath, RegistryValueKind.String);

        productNameKey.SetValue("version", StandardInstallContext.AppVersion, RegistryValueKind.String);

        productNameKey.SetValue("VersionPath", StandardInstallContext.MainInstallPath, RegistryValueKind.String);
    }

    /// <summary>
    /// 写卸载注册表
    /// </summary>
    private void WriteUninstallRegister()
    {
        // 注册表卸载项路径
        // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\
        var uninstallKey =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                writable: true)!;
        var code = StandardInstallContext.ProductCodeGuid.ToString("B");
        var productUninstallKey = uninstallKey.CreateSubKey(code);

        var icon = StandardInstallContext.GetUninstallDisplayIconFullPath();
        if (!string.IsNullOrEmpty(icon))
        {
            productUninstallKey.SetValue("DisplayIcon", icon, RegistryValueKind.String);
        }

        productUninstallKey.SetValue("DisplayName", StandardInstallContext.DisplayProductName, RegistryValueKind.String);

        productUninstallKey.SetValue("DisplayVersion", StandardInstallContext.UninstallDisplayVersion, RegistryValueKind.String);

        var size = StandardInstallContext.UninstallEstimatedSize;
        if (size is null)
        {

        }

        if (size is not null)
        {
            productUninstallKey.SetValue("EstimatedSize", size,RegistryValueKind.DWord);
        }

        productUninstallKey.SetValue("Publisher", StandardInstallContext.UninstallDisplayPublisher, RegistryValueKind.String);

        var uninstaller = StandardInstallContext.GetUninstallerFullPath();
        if (!string.IsNullOrEmpty(uninstaller))
        {
            productUninstallKey.SetValue("UninstallString", uninstaller, RegistryValueKind.String);
        }
    }
}