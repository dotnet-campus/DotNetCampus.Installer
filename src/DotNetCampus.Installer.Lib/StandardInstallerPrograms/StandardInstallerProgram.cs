using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.SplashScreens;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器流程
/// </summary>
[SupportedOSPlatform("windows5.0")]
public abstract class StandardInstallerProgram : IDisposable
{
    public abstract StandardInstallContext StandardInstallContext { get; }

    #region 环境

    /// <summary>
    /// 检测环境和弹出提示
    /// </summary>
    /// <returns></returns>
    public virtual bool CheckEnvironment()
    {
        var isSingleton = CheckSingletonInstaller();
        if (!isSingleton)
        {
            // 本产品已经有一个安装向导正在运行！
            PInvoke.MessageBox(GetMessageOwner(), "本产品已经有一个安装向导正在运行！", StandardInstallContext.DisplayProductName,
                MESSAGEBOX_STYLE.MB_ICONWARNING);

            return false;
        }

        return EnvironmentChecker.CheckEnvironmentAndShowMessageBox();
    }

    protected bool CheckSingletonInstaller()
    {
        var mutexName = StandardInstallContext.SingletonMutexName;
        _singletonMutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        return createdNew;
    }

    /// <summary>
    /// 单例用的互斥锁
    /// </summary>
    private Mutex? _singletonMutex;

    #endregion

    #region 欢迎界面

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

    #endregion

    #region 安装

    /// <summary>
    /// 安装
    /// </summary>
    public virtual void Install()
    {
        // 安装过程：
        // 1. 清理旧版本
        //   1.1. 先查看是否有旧版本
        //   1.2. 如果有旧版本，先卸载旧版本
        //   1.3. 如果有相同版本，先杀进程，后删除文件，再安装新版本
        // 2. 解压缩文件到安装路径
        // 3. 写注册表和快捷方式

        // 1. 清理旧版本
        ClearOldVersion();

        // 2. 解压缩文件到安装路径
        Decompress();

        // 3. 写注册表和快捷方式
        WriteRegister();
    }

    /// <summary>
    /// 清理旧版本
    /// </summary>
    private void ClearOldVersion()
    {
        // 可选检测旧版本
        var oldVersion = ReadVersionFromRegister();
        var currentVersion = StandardInstallContext.AppVersion;
        if (oldVersion is not null)
        {
            if (oldVersion == currentVersion)
            {
                // 给出提示，覆盖安装
                PInvoke.MessageBox(GetMessageOwner(), "检测到系统中已安装相同版本的程序，安装程序将覆盖安装该版本。", StandardInstallContext.DisplayProductName,
                    MESSAGEBOX_STYLE.MB_ICONWARNING);
            }
        }
    }

    #region 解压缩

    /// <summary>
    /// 解压缩，将 <see cref="StandardInstallContext.ContentResourceAssetsInfo"/> 解压缩到安装路径下
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual void Decompress()
    {
        var contentResourceAssetsInfo = StandardInstallContext.ContentResourceAssetsInfo;
        if (contentResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        var mainInstallPath = StandardInstallContext.MainInstallPath;
        using var stream = contentResourceAssetsInfo.Value.GetManifestResourceStream();
        DirectoryArchive.Decompress(stream,
            Directory.CreateDirectory(mainInstallPath));
    }

    #endregion

    #region 注册表

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

    private string? ReadVersionFromRegister()
    {
        // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node
        var softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", writable: false)!;
        var productNameKey = softwareKey.OpenSubKey(@$"{StandardInstallContext.ProductFamily}\{StandardInstallContext.ProductName}");
        if (productNameKey != null)
        {
            return productNameKey.GetValue("version") as string;
        }

        return null;
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
            productUninstallKey.SetValue("EstimatedSize", size, RegistryValueKind.DWord);
        }

        productUninstallKey.SetValue("Publisher", StandardInstallContext.UninstallDisplayPublisher, RegistryValueKind.String);

        var uninstaller = StandardInstallContext.GetUninstallerFullPath();
        if (!string.IsNullOrEmpty(uninstaller))
        {
            productUninstallKey.SetValue("UninstallString", uninstaller, RegistryValueKind.String);
        }
    }

    #endregion

    #region 快捷方式



    #endregion

    #endregion

    private HWND GetMessageOwner()
    {
        if (StandardInstallContext.InstallerUIWindowHandler == 0)
        {
            return HWND.Null;
        }

        return new HWND(StandardInstallContext.InstallerUIWindowHandler);
    }

    public void Dispose()
    {
        _singletonMutex?.Dispose();
    }
}