using Avalonia.Controls;
using DotNetCampus.Installer.AvaloniaSample.Foundation;

namespace DotNetCampus.Installer.AvaloniaSample;
public partial class MainWindow : Window
{
    public MainWindow(AppInfo appInfo)
    {
        AppInfo = appInfo;
        InitializeComponent();
    }

    public AppInfo AppInfo { get; }
}