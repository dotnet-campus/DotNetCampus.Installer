using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;
using DotNetCampus.Installer.Lib.Exceptions;
using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DotNetCampus.Installer.AvaloniaClassic;

internal static class Program
{
    [STAThread]
    // 这里强行异步转同步，而不是 async 的原因是为了避免弄坏 STAThread 特性
    // https://github.com/dotnet/runtime/issues/73099
    public static int Main(string[] args)
    {
        var program = InitAsync().Result;

        if (!program.CheckEnvironment())
        {
            return -1;
        }

        return RunAvalonia(args, program);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int RunAvalonia(string[] args, ClassicInstallerProgram? installerProgram = null)
        {
            return BuildAvaloniaApp(installerProgram).StartWithClassicDesktopLifetime(args);
        }
    }

    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(null);

    public static AppBuilder BuildAvaloniaApp(ClassicInstallerProgram? installerProgram)
    {
        var appBuilder = AppBuilder.Configure<App>(()=> new App(installerProgram))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        appBuilder.With(new FontManagerOptions()
        {
            DefaultFamilyName = "Microsoft YaHei UI",
            FontFallbacks =
            [
                new FontFallback { FontFamily = "Microsoft YaHei" },
            ],
        });
        return appBuilder;
    }

    private static async Task<ClassicInstallerProgram> InitAsync()
    {
        var directoryArchive = await GetDirectoryArchiveAsync();

        // 创建工作文件夹路径
        var tempPath = Path.GetTempPath();
        var workingFolder = Path.Join(tempPath, $"Installer_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(workingFolder);

        // 读取配置文件和 Avalonia 的所需文件
        await LoadNativeLibrary(directoryArchive, workingFolder, "libSkiaSharp.dll");
        await LoadNativeLibrary(directoryArchive, workingFolder, "libHarfBuzzSharp.dll");

        // 读取安装程序配置文件
        var appConfigurator = await LoadInstallerConfigurationAsync(directoryArchive);
        var installerConfiguration = appConfigurator.Of<StandardInstallerConfiguration>();
        var installContext = installerConfiguration.CreateInstallContext(directoryArchive, new DirectoryInfo(workingFolder));
        return new ClassicInstallerProgram(installContext, appConfigurator);
    }

    /// <summary>
    /// 读取安装程序配置文件
    /// </summary>
    /// <param name="directoryArchive"></param>
    /// <returns></returns>
    private static async Task<IAppConfigurator> LoadInstallerConfigurationAsync(IDirectoryArchive directoryArchive)
    {
        IDirectoryArchiveEntryFile installerConfigurationEntryFile = directoryArchive.GetEntryFile("Installer.coin");
        using var memoryStream = new MemoryStream();
        await installerConfigurationEntryFile.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var streamReader = new StreamReader(memoryStream);
        var text = await streamReader.ReadToEndAsync();
        var configuration = CoinConfigurationSerializer.Deserialize(text);
        var memoryConfigurationRepo = new MemoryConfigurationRepo(configuration);
        var appConfigurator = memoryConfigurationRepo.CreateAppConfigurator();
        return appConfigurator;
    }

    private static async Task<IDirectoryArchive> GetDirectoryArchiveAsync()
    {
        var reader = new PEOverlayContentReader();
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath != null);
        OverlayInstallerContentInfo? readOverlayInstallerContent = await reader.ReadOverlayInstallerContent(new FileInfo(processPath), new InstallerLogger());

        if (readOverlayInstallerContent is null)
        {
#if DEBUG
            // 没有找到 OverlayInstallerContentInfo，说明不是 Overlay 安装器，但可以是在调试下，为了提升调试体验，是允许为空的情况，此时可以读取本地的某个文件夹路径
            var debugInstallerContentFolder = Path.Join(AppContext.BaseDirectory, "DebugContent");
            // 请将 debugInstallerContentFolder 替换为你自己的路径
            if (!Directory.Exists(debugInstallerContentFolder))
            {
                // 提醒一下开发者设置
                Debugger.Break();
            }
            else
            {
                var fakeDirectoryArchive = new FakeDirectoryArchive(new DirectoryInfo(debugInstallerContentFolder));
                return fakeDirectoryArchive;
            }
#endif
            PInvoke.MessageBox(HWND.Null, $"安装包内容损坏，无法获取到安装包内容", "安装包损坏", MESSAGEBOX_STYLE.MB_OK);
            throw new InstallerException($"安装包内容损坏，无法获取到安装包内容");
        }
        else
        {
            var overlayInstallerContentStream = readOverlayInstallerContent.Value.ContentStream;

            var directoryArchive = await DirectoryArchive.OpenReadAsync(overlayInstallerContentStream);
            return directoryArchive;
        }
    }

    static async Task LoadNativeLibrary(IDirectoryArchive directoryArchive, string libFolder, string libraryName)
    {
        var libraryEntryFile = directoryArchive.GetEntryFile(libraryName);
        var libraryOutputPath = Path.Join(libFolder, libraryName);
        await libraryEntryFile.SaveToFileAsync(new FileInfo(libraryOutputPath));
        NativeLibrary.Load(libraryOutputPath);
    }
}
