using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DotNetCampus.Installer.AvaloniaSample.Foundation;

namespace DotNetCampus.Installer.AvaloniaSample;
public partial class App : Application
{
    public App(AppInfo? appInfo)
    {
        AppInfo = appInfo ?? new AppInfo();
    }

    public AppInfo AppInfo { get; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(AppInfo);
        }

        base.OnFrameworkInitializationCompleted();
    }
}