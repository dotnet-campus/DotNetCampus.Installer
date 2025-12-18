using Avalonia.Controls;
using Avalonia.Input;
using DotNetCampus.Installer.AvaloniaSample.Foundation;
using DotNetCampus.Installer.Lib.Hosts;

namespace DotNetCampus.Installer.AvaloniaSample;
public partial class MainWindow : Window
{
    public MainWindow(AppInfo appInfo)
    {
        AppInfo = appInfo;
        InitializeComponent();

     
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
        base.OnPointerPressed(e);
    }

    public AppInfo AppInfo { get; }
}