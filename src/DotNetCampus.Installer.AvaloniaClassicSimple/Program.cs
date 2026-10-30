using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using DotNetCampus.Installer.Lib.Exceptions;
using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.AvaloniaClassicSimple;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        using var program = InitializeAsync().GetAwaiter().GetResult();
        if (!program.CheckEnvironment()) return -1;
        var result = program.RunDefaultCommandLine(args).GetAwaiter().GetResult();
        return result.ShouldExitsInstallerProcess ? result.ExitCode : RunAvalonia(args, program);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunAvalonia(string[] args, ClassicInstallerProgram program)
    {
        App.InstallerProgram = program;
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }

    private static async Task<ClassicInstallerProgram> InitializeAsync()
    {
        var overlay = await new PEOverlayContentReader().ReadOverlayInstallerContent(
            new FileInfo(Environment.ProcessPath ?? throw new InvalidOperationException()), new InstallerLogger());
        if (overlay is null) throw new InstallerException(LocalizedText.Current.Corrupted);
        var archive = await DirectoryArchive.OpenReadAsync(overlay.Value.ContentStream);
        var working = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}"));
        foreach (var name in new[] { "libSkiaSharp.dll", "libHarfBuzzSharp.dll" })
        {
            var path = Path.Join(working.FullName, name);
            await archive.GetEntryFile(name).SaveToFileAsync(new FileInfo(path));
            NativeLibrary.Load(path);
        }
        using var memory = new MemoryStream();
        await archive.GetEntryFile("Installer.coin").CopyToAsync(memory);
        memory.Position = 0;
        using var reader = new StreamReader(memory);
        var configurator = new MemoryConfigurationRepo(CoinConfigurationSerializer.Deserialize(await reader.ReadToEndAsync())).CreateAppConfigurator();
        var config = configurator.Of<ClassicInstallerConfiguration>();
        if (!string.IsNullOrWhiteSpace(config.ProductIconPath))
        {
            var iconPath = Path.Join(working.FullName, Path.GetFileName(config.ProductIconPath));
            await archive.GetEntryFile(config.ProductIconPath).SaveToFileAsync(new FileInfo(iconPath));
            config.ProductIconPath = iconPath;
        }
        return new ClassicInstallerProgram(configurator.Of<StandardInstallerConfiguration>().CreateInstallContext(archive, working), configurator);
    }
}
