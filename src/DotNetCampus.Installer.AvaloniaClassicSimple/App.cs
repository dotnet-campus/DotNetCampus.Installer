using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Media.Imaging;
using dotnetCampus.Configurations;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassicSimple;

/// <summary>简洁安装界面应用程序。</summary>
public sealed class App : Application
{
    internal static ClassicInstallerProgram? InstallerProgram { get; set; }
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && InstallerProgram is { } program)
        {
            var path = program.AppConfigurator.Of<ClassicInstallerConfiguration>().ProductIconPath;
            Bitmap? icon = !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? new Bitmap(path) : null;
            desktop.MainWindow = new MainWindow(InstallerViewModel.FromProgram(program, icon), program);
            desktop.MainWindow.Closed += (_, _) => icon?.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
