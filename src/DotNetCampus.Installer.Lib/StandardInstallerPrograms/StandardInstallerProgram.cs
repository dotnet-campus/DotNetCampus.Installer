using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.Installer.Lib.SplashScreens;
using Microsoft.Win32;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器流程
/// </summary>
public abstract class StandardInstallerProgram
{
    public abstract StandardInstallContext StandardInstallContext { get; }

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
        var softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE")!;

        var productFamilyKey =
            softwareKey.CreateSubKey(StandardInstallContext.ProductFamily);
    }

    /// <summary>
    /// 写卸载注册表
    /// </summary>
    private void WriteUninstallRegister()
    {
        // 注册表卸载项路径
        // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\
    }
}