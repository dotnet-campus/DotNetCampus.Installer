using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class App : Application
{
    public App() : this(null)
    {
    }

    private readonly ClassicInstallerProgram? _installerProgram;

    public App(ClassicInstallerProgram? installerProgram)
    {
        _installerProgram = installerProgram;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(_installerProgram);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
