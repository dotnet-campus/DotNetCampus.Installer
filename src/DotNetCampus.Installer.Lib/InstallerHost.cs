using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Lib.Commandlines;
using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.Hosts;
using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.Installer.Lib.SplashScreens;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Utils;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib;

#pragma warning disable CA1416 // 执行版本有 YY-Thunks 保底，不适用文档描述的要求版本号

/// <summary>
/// 安装器主机
/// </summary>
/// 此对象可以继承和重写里面的很多方法来实现自定义的安装器主机行为。默认情况下，都是将独立的安装器打包作为资源，启动的时候解压缩出来运行。默认的情况的方法会导致安装包启动速度比较慢，毕竟需要解压缩和被杀毒扫描。自定义安装器可以直接立刻运行，但要求 UI 界面安装程序能够支持 AOT 构建
/// 但通常采用非 Boost 方式将使用 <see cref="StandardInstallerProgram"/> 标准安装过程，而不是采用主机方式
public class InstallerHost
{
    public static InstallerHostBuilder CreateBuilder()
    {
        return new InstallerHostBuilder();
    }

    /// <summary>
    /// 创建安装器主机
    /// </summary>
    public InstallerHost(InstallerHostConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 安装器的配置
    /// </summary>
    public InstallerHostConfiguration Configuration => _configuration;

    private readonly InstallerHostConfiguration _configuration;

    /// <summary>
    /// 运行安装器主机
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            return -1;
        }

        var environmentSuccess = CheckEnvironment();
        if (!environmentSuccess)
        {
            return -1;
        }

        if (_configuration.SplashScreenFile is not null)
        {
            ShowSplashScreen();
        }
        else
        {
            var context = CreateInstallContext(IntPtr.Zero);
            Install(context)
                .Wait();
        }

        return 0;
    }

    private InstallContext CreateInstallContext(IntPtr splashScreenWindowHandler)
    {
        return new InstallContext()
        {
            SplashScreenWindowHandler = splashScreenWindowHandler,
            WorkingFolder = _configuration.WorkingFolder,
            ContentResourceAssetsInfo = _configuration.ContentResourceAssetsInfo,
        };
    }

    /// <summary>
    /// 检测环境和弹出提示
    /// </summary>
    /// <returns></returns>
    protected virtual bool CheckEnvironment()
    {
        return EnvironmentChecker.CheckEnvironmentAndShowMessageBox();
    }

    private void ShowSplashScreen()
    {
        if (_configuration.SplashScreenFile == null)
        {
            return;
        }

        var splashScreen = new SplashScreen(_configuration.SplashScreenFile);

        splashScreen.Showed += (s, eventArgs) =>
        {
            // 等待欢迎界面启动完成了，再继续执行后续代码，确保欢迎窗口足够快显示
            var thread = new Thread(() =>
            {
                try
                {
                    var context = CreateInstallContext(eventArgs.SplashScreenWindowHandler);
                    _ = Install(context);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            })
            {
                IsBackground = false,// 需要是前台窗口，确保主线程退出之后，当前工作线程依然还能继续运行。只要有一个线程还在运行，进程就不会退出。为什么需要这样做？因为很多应用软件管理器都会依靠其调起的安装包进程是否退出来判断是否安装完成
            };
            thread.Start();
        };

        splashScreen.Show();
    }

    /// <summary>
    /// 开始安装
    /// </summary>
    /// <param name="context"></param>
    protected virtual async Task Install(InstallContext context)
    {
        await InstallByIndependentInstallerProcess(context);
    }

    /// <summary>
    /// 通过独立的安装器进程来安装
    /// </summary>
    /// <param name="context"></param>
    protected async Task InstallByIndependentInstallerProcess(InstallContext context)
    {
        var workingFolder = _configuration.WorkingFolder;
        string installerApplicationFile;

#if DEBUG
        var debugInstallerFile =
            @"..\..\..\..\DotNetCampus.Installer.Sample\bin\Debug\net9.0-windows\DotNetCampus.Installer.Sample.exe";
        debugInstallerFile = Path.GetFullPath(debugInstallerFile);
        if (File.Exists(debugInstallerFile))
        {
            installerApplicationFile = debugInstallerFile;
        }
        else
#endif
        {
            installerApplicationFile = await ExtractInstallerAssets();
        }

        List<string> argumentList =
        [
            InstallOptions.VerbName,// verb

            // 传入当前安装包启动器的 PID 也许安装包界面程序有用
            $"--{InstallOptions.BoostPidOptionName}",
            Environment.ProcessId.ToString(),
        ];

        var splashScreenWindowHandler = context.SplashScreenWindowHandler;
        if (splashScreenWindowHandler != IntPtr.Zero)
        {
            // 传入欢迎界面的句柄，安装包会在安装界面开始时欢迎界面
            argumentList.Add($"--{InstallOptions.SplashScreenWindowHandlerOptionName}");
            argumentList.Add(splashScreenWindowHandler.ToInt64().ToString());
        }

        var processStartInfo = new ProcessStartInfo(installerApplicationFile, argumentList);
        var processContext = new ProcessStartInfoConfigurationContext()
        {
            ProcessStartInfo = processStartInfo,
            WorkingFolder = workingFolder,
            SplashScreenWindowHandler = splashScreenWindowHandler
        };
        _configuration.InstallerProcessStartConfigAction?.Invoke(processContext);

        var process = Process.Start(processStartInfo)!;
        process.WaitForExit();

        // 尝试清理工作文件夹
        FolderDeleteHelper.DeleteFolder(workingFolder.FullName);

        Environment.Exit(process.ExitCode);
    }

    private async Task<string> ExtractInstallerAssets()
    {
        if (_configuration.InstallerResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        var workingFolder = _configuration.WorkingFolder;
        var installerResourceAssetsInfo = _configuration.InstallerResourceAssetsInfo.Value;
        using var assetsStream = installerResourceAssetsInfo.GetManifestResourceStream();
        var resourceAssetsFolder = Directory.CreateDirectory(Path.Join(workingFolder.FullName, installerResourceAssetsInfo.ManifestResourceName));

        await DirectoryArchive.DecompressAsync(assetsStream, resourceAssetsFolder);

        // 带界面的安装包界面程序
        var installerApplicationFile = Path.Join(resourceAssetsFolder.FullName, _configuration.InstallerRelativePath);

        if (!File.Exists(installerApplicationFile))
        {
            throw new FileNotFoundException(
                $"无法找到 {installerResourceAssetsInfo.ManifestResourceName} 里的 {_configuration.InstallerRelativePath} 安装器文件", installerApplicationFile);
        }

        return installerApplicationFile;
    }
}