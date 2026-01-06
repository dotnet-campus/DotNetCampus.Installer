using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.SplashScreens;
using DotNetCampus.Installer.Lib.Utils;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Lib.Logging;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器流程
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public abstract class StandardInstallerProgram : IDisposable
{
    public abstract StandardInstallContext StandardInstallContext { get; }
    public InstallerLogger Logger => StandardInstallContext.Logger;

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
    public virtual async Task InstallAsync()
    {
        // 安装过程：
        // 1. 清理旧版本
        //   1.1. 先查看是否有旧版本
        //   1.2. 如果有旧版本，先卸载旧版本
        //   1.3. 如果有相同版本，先杀进程，后删除文件，再安装新版本
        // 2. 解压缩文件到安装路径
        // 3. 写注册表和快捷方式

        Logger.WriteLog($"Install start. Installer PID={Environment.ProcessId}");

        // 1. 清理旧版本
        ClearOldVersion();

        // 清理旧版本之后才能重新创建安装目录
        Directory.CreateDirectory(StandardInstallContext.MainInstallPath);
        // 创建了安装目录之后，才能创建日志文件
        var logFile = new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, "InstallerLog.txt"));
        Logger.SetLogFile(logFile);

        // 2. 解压缩文件到安装路径
        await Decompress();

        // 3. 写注册表和快捷方式
        WriteRegister();
        CreateShortcut();
    }

    /// <summary>
    /// 清理旧版本
    /// </summary>
    protected void ClearOldVersion()
    {
        Logger.WriteLog($"Start ClearOldVersion");

        // 可选检测旧版本
        var oldVersion = ReadVersionFromRegister();

        Logger.WriteLog($"Read Old version. OldVersion='{oldVersion}'");

        var currentVersion = StandardInstallContext.AppVersion;
        if (oldVersion is not null)
        {
            if (oldVersion == currentVersion)
            {
                // 给出提示，覆盖安装
                PInvoke.MessageBox(GetMessageOwner(), "检测到系统中已安装相同版本的程序，安装程序将覆盖安装该版本。", StandardInstallContext.DisplayProductName,
                    MESSAGEBOX_STYLE.MB_ICONWARNING);
            }
            else
            {
                // 如果能作为版本判断的话，用版本判断
                if (Version.TryParse(oldVersion, out var oldVersionValue) && Version.TryParse(currentVersion, out var currentVersionValue))
                {
                    if (currentVersionValue < oldVersionValue)
                    {
                        PInvoke.MessageBox(GetMessageOwner(), $"检测到系统中已安装更新版本 {oldVersion} 的程序，安装程序将降级已安装版本。", StandardInstallContext.DisplayProductName,
                            MESSAGEBOX_STYLE.MB_ICONWARNING);
                    }
                }
            }

            // 尝试调用旧版本的卸载功能
            var (oldVersionUninstallerPath, oldVersionUninstallerArgument) = GetOldVersionUninstaller();
            if (!string.IsNullOrEmpty(oldVersionUninstallerPath))
            {
                var process = Process.Start(oldVersionUninstallerPath, oldVersionUninstallerArgument);
                process.WaitForExit();
            }
        }

        var installRootPath = StandardInstallContext.InstallRootPath;
        if (Directory.Exists(installRootPath))
        {
            Logger.WriteLog($"Delete Install Path. Path={installRootPath}");
            Logger.WriteLog($"Start KillProcessInInstallPath");

            // 结束旧版本进程和清理
            KillProcessInInstallPath();
            Logger.WriteLog($"Finish KillProcessInInstallPath");

            // 删除安装路径
            Logger.WriteLog($"Start Delete '{installRootPath}'");
            FolderDeleteHelper.DeleteFolder(installRootPath);
            Logger.WriteLog($"Finish Delete '{installRootPath}'");
        }
    }

    /// <summary>
    /// 杀掉所有在安装路径下运行的进程
    /// </summary>
    protected void KillProcessInInstallPath()
    {
        // 重复杀3次，防止进程相互拉起
        for (int i = 0; i < 3; i++)
        {
            try
            {
                KillCore();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }


        void KillCore()
        {
            var installRootPath = StandardInstallContext.InstallRootPath;

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id == 0)
                    {
                        // 这是 Idle 进程，忽略
                        continue;
                    }

                    if (process.Id == 4)
                    {
                        // 这是 System 进程，忽略
                        continue;
                    }

                    var fileName = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(fileName) &&
                        fileName.StartsWith(installRootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // 进程包含在安装路径下，结束它
                        process.Kill();
                    }
                }
                catch (Win32Exception e)
                {
                    if (e.NativeErrorCode == 0x5)
                    {
                        // 拒绝访问
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
    }

    #region 解压缩

    /// <summary>
    /// 解压缩，将放在 Overlay 里的内容或 <see cref="StandardInstallContext.ContentResourceAssetsInfo"/> 内容解压缩到安装路径下
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual async Task Decompress()
    {
        // 优先使用 Overlay 里的内容
        // 优势在于： 可以一次做好安装包，之后不需要重复构建，只需要每次在模版 exe 添加 Overlay 内容就可以了
        // 其次是可以突破 PE 文件的 2 GB 大小限制
        // 再次是可以减少工作集的大小，在 Windows 里面，不会加载 Overlay 里的内容到内存中
        // 但带来的缺点是杀毒软件会扫描更久一点，且最好是添加数字签名，否则杀毒软件会误报。数字签名将放在 Overlay 之后，因此需要先添加 Overlay 内容，让数字签名作为最后步骤
        var directoryArchive = await StandardInstallContext.GetOverlayDirectoryArchive();
        if (directoryArchive is not null)
        {
            Logger.WriteLog($"Decompress from Overlay to '{StandardInstallContext.MainInstallPath}'");

            // 按照约定，取 Packing\ 路径下的内容进行解压缩
            bool anyPackingContent = false;
            foreach (var directoryArchiveEntryFile in directoryArchive.EntryFileList)
            {
                const string packingPrefix = @"Packing\";
                if (directoryArchiveEntryFile.RelativePath.StartsWith(packingPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = directoryArchiveEntryFile.RelativePath.Substring(packingPrefix.Length);
                    var outputFile = new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, relativePath));

                    outputFile.Directory?.Create();

                    await directoryArchiveEntryFile.SaveToFileAsync(outputFile);

                    anyPackingContent = true;
                }
            }

            if (!anyPackingContent)
            {
                // 没有任何加入到安装包里的内容
                throw new InvalidOperationException();
            }
        }

        var contentResourceAssetsInfo = StandardInstallContext.ContentResourceAssetsInfo;
        if (contentResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        var mainInstallPath = StandardInstallContext.MainInstallPath;
        using var stream = contentResourceAssetsInfo.Value.GetManifestResourceStream();
        await DirectoryArchive.DecompressAsync(stream,
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
    /// 获取旧版本的卸载命令行
    /// </summary>
    /// <returns></returns>
    private (string UninstallerPath, string UninstallerArgument) GetOldVersionUninstaller()
    {
        // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\
        var softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", writable: false)!;
        var code = StandardInstallContext.ProductCodeGuid.ToString("B");
        var uninstallKey = softwareKey.OpenSubKey($@"Microsoft\Windows\CurrentVersion\Uninstall\{code}");

        if (uninstallKey != null)
        {
            var uninstallString = uninstallKey.GetValue("UninstallString") as string;
            return SplitCommandLine(uninstallString);
        }

        return default;

        (string Path, string Argument) SplitCommandLine(string? command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return default;
            }

            var start = 0;
            var end = 0;
            bool isIncludeQuote = false;

            for (var i = 0; i < command.Length; i++)
            {
                if (command[i] == '\"')
                {
                    isIncludeQuote = !isIncludeQuote;
                }
                else if (command[i] == ' ' && !isIncludeQuote)
                {
                    if (start < end)
                    {
                        break;
                    }
                    start = end + 1;
                }
                end++;
            }

            var path = command[start..end].Trim('\"');
            var argument = command[end..].Trim();

            return (path, argument);
        }
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

        productUninstallKey.SetValue("DisplayName", StandardInstallContext.UninstallDisplayName, RegistryValueKind.String);

        productUninstallKey.SetValue("DisplayVersion", StandardInstallContext.UninstallDisplayVersion, RegistryValueKind.String);

        var size = StandardInstallContext.UninstallEstimatedSize;
        if (size is null)
        {
            // 可以考虑统计一下安装目录的大小，将其作为安装之后的体积
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

    /// <summary>
    /// 创建桌面快捷方式和开始菜单快捷方式
    /// </summary>
    protected virtual void CreateShortcut()
    {
        var launcherExeFullPath = StandardInstallContext.GetLauncherExeFullPath();

        if (string.IsNullOrEmpty(launcherExeFullPath))
        {
            // 没有可以启动的程序，无法创建快捷方式
            return;
        }

        var name = StandardInstallContext.DisplayProductName;
        var workDir = StandardInstallContext.MainInstallPath;

        // 默认应该放在公共的桌面上，不能放在当前用户桌面上，因为安装包本身的权限不一定是当前用户
        var shortcutFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            $"{name}.lnk");
        CreateShortcut(shortcutFile, launcherExeFullPath, workDir);

        // 可以考虑创建开始菜单快捷方式
        string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        string programsPath = Path.Join(commonStartMenuPath, "Programs");
        var displayProductFamily = StandardInstallContext.DisplayProductFamily;
        var startMenuShortcutFolder = Path.Join(programsPath, displayProductFamily);
        Directory.CreateDirectory(startMenuShortcutFolder);
        var shortcutFileInStartMenu = Path.Join(startMenuShortcutFolder, $"{name}.lnk");
        File.Copy(shortcutFile, shortcutFileInStartMenu, overwrite: true);
    }

    #endregion

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建快捷方式
    /// </summary>
    /// <param name="lnkFilePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="workDir"></param>
    /// <param name="args"></param>
    /// <param name="iconFile"></param>
    /// 此方法仅仅只是为了方便开发者调用
    protected void CreateShortcut(string lnkFilePath, string targetPath, string workDir, string args = "",
        string iconFile = "")
    {
        ShortcutHelper.CreateShortcut(lnkFilePath, targetPath, workDir, args, iconFile);
    }

    /// <summary>
    /// 降权启动。默认安装包使用管理员权限启动，如果此时有某些应用程序需要以普通用户权限启动，可以使用此方法启动。此方法将取 explorer.exe 的权限启动新进程，从而达到降权启动的目的
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    /// 此方法仅仅只是为了方便开发者调用
    protected bool StartProcessWithShellProcessToken(string fileName, string? arguments = null)
    {
        return ProcessRunner.StartProcessWithShellProcessToken(fileName, arguments, Logger);
    }

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