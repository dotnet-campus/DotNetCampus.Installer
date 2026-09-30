using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaSample;

public partial class App : Application
{
    public App(InstallerProgram? installerProgram = null)
    {
        InstallerProgram = installerProgram ?? new ();
    }

    public InstallerProgram InstallerProgram { get; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(InstallerProgram);
        }

        base.OnFrameworkInitializationCompleted();
    }
}