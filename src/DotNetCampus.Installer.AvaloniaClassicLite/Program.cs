using Avalonia;
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
    // 保持桌面入口的 STA 线程，不能改为异步 Main。
    public static int Main(string[] args)
    {
        var program = InitAsync().Result;
        if (!program.CheckEnvironment()) return -1;
        var runResult = program.RunDefaultCommandLine(args).Result;
        if (runResult.ShouldExitsInstallerProcess) return runResult.ExitCode;
        return RunAvalonia(args, program);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int RunAvalonia(string[] args, ClassicInstallerProgram installerProgram)
            => BuildAvaloniaApp(installerProgram).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(null);

    public static AppBuilder BuildAvaloniaApp(ClassicInstallerProgram? installerProgram)
        => AppBuilder.Configure<App>(() => new App(installerProgram))
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "Microsoft YaHei UI",
                FontFallbacks = [new FontFallback { FontFamily = "Microsoft YaHei" }],
            })
            .LogToTrace();

    private static async Task<ClassicInstallerProgram> InitAsync()
    {
        var directoryArchive = await GetDirectoryArchiveAsync();
        var workingFolder = Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(workingFolder);
        await LoadNativeLibraryAsync(directoryArchive, workingFolder, "libSkiaSharp.dll");
        await LoadNativeLibraryAsync(directoryArchive, workingFolder, "libHarfBuzzSharp.dll");
        var appConfigurator = await LoadInstallerConfigurationAsync(directoryArchive);
        var installerConfiguration = appConfigurator.Of<StandardInstallerConfiguration>();
        var installContext = installerConfiguration.CreateInstallContext(directoryArchive, new DirectoryInfo(workingFolder));
        var iconRelativePath = appConfigurator.Of<ClassicInstallerConfiguration>().IconRelativePath;
        if (!string.IsNullOrWhiteSpace(iconRelativePath))
        {
            // 使用固定临时文件名，避免将配置中的归档路径用作磁盘输出路径。
            var iconEntry = directoryArchive.GetEntryFile(iconRelativePath);
            await iconEntry.SaveToFileAsync(new FileInfo(Path.Join(workingFolder, "InstallerIcon")));
        }
        return new ClassicInstallerProgram(installContext, appConfigurator);
    }

    private static async Task<IAppConfigurator> LoadInstallerConfigurationAsync(IDirectoryArchive directoryArchive)
    {
        var entry = directoryArchive.GetEntryFile("Installer.coin");
        using var memoryStream = new MemoryStream();
        await entry.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        using var streamReader = new StreamReader(memoryStream);
        var text = await streamReader.ReadToEndAsync();
        var configuration = CoinConfigurationSerializer.Deserialize(text);
        return new MemoryConfigurationRepo(configuration).CreateAppConfigurator();
    }

    private static async Task<IDirectoryArchive> GetDirectoryArchiveAsync()
    {
        var reader = new PEOverlayContentReader();
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath != null);
        var content = await reader.ReadOverlayInstallerContent(new FileInfo(processPath), new InstallerLogger());
        if (content is not null)
            return await DirectoryArchive.OpenReadAsync(content.Value.ContentStream);
#if DEBUG
        var debugInstallerContentFolder = Path.Join(AppContext.BaseDirectory, "DebugContent");
        if (Directory.Exists(debugInstallerContentFolder))
            return new FakeDirectoryArchive(new DirectoryInfo(debugInstallerContentFolder));
        Debugger.Break();
#endif
        PInvoke.MessageBox(HWND.Null, InstallerMessages.CorruptedPackageMessage, InstallerMessages.CorruptedPackageTitle, MESSAGEBOX_STYLE.MB_OK);
        throw new InstallerException(InstallerMessages.CorruptedPackageMessage);
    }

    private static async Task LoadNativeLibraryAsync(IDirectoryArchive directoryArchive, string libFolder, string libraryName)
    {
        var entry = directoryArchive.GetEntryFile(libraryName);
        var outputPath = Path.Join(libFolder, libraryName);
        await entry.SaveToFileAsync(new FileInfo(outputPath));
        NativeLibrary.Load(outputPath);
    }
}
