using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.AvaloniaSample.ViewModels;
using DotNetCampus.Installer.Lib.Hosts;

namespace DotNetCampus.Installer.AvaloniaSample;
public partial class MainWindow : Window
{
    public MainWindow(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
        ViewModel = new MainViewModel(installerProgram);
        DataContext = ViewModel;
     
        InitializeComponent();

        InstallBar.RequestInstallStart += InstallBar_RequestInstallStart;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (TryGetPlatformHandle() is {} handle)
        {
            InstallerProgram.StandardInstallContext.InstallerUIWindowHandler = handle.Handle;
        }
    }

    public MainViewModel ViewModel { get; }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
        base.OnPointerPressed(e);
    }

    public InstallerProgram InstallerProgram { get; }

    private async void InstallBar_RequestInstallStart(object? sender, System.EventArgs e)
    {
        try
        {
            InstallBar.IsVisible = false;
            InstallStatus.IsVisible = true;

            // 点击了开始安装的按钮，现在开始安装
            // 需要切换一下界面
            await Task.Run(() => InstallerProgram.Install());

            InstallStatus.IsVisible = false;
            InstallFinish.IsVisible = true;
        }
        catch (Exception exception)
        {
            // async void 捕获全部异常
            Debug.WriteLine(exception);
        }
    }
}