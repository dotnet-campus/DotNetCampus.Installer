using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class App : Application
{
    private readonly ClassicInstallerProgram? _installerProgram;
    /// <summary>创建可用于设计和无头测试的应用。</summary>
    public App() { }
    /// <summary>使用实际安装上下文创建应用。</summary>
    public App(ClassicInstallerProgram? installerProgram) => _installerProgram = installerProgram;
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow(_installerProgram);
        base.OnFrameworkInitializationCompleted();
    }
}
