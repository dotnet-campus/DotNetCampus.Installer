using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.AvaloniaSample.ViewModels;

namespace DotNetCampus.Installer.AvaloniaSample;

public partial class MainWindow : Window
{
    public MainWindow(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
        ViewModel = new MainViewModel(installerProgram);
        DataContext = ViewModel;

        var installContext = installerProgram.StandardInstallContext;
     
        InitializeComponent();

        Title = $"{installContext.DisplayProductName} 安装程序";

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

    public InstallerProgram InstallerProgram { get; }

    private async void InstallBar_RequestInstallStart(object? sender, System.EventArgs e)
    {
        try
        {
            InstallBar.IsVisible = false;
            InstallStatusControl.IsVisible = true;
            ViewModel.InstallStatus = InstallStatus.Installing;

            // 点击了开始安装的按钮，现在开始安装
            // 需要切换一下界面
            await Task.Run(async () =>
            {
                try
                {
                    await InstallerProgram.InstallAsync();
                }
                catch (Exception exception)
                {
                    InstallerProgram.Logger.WriteLog($"[Error] Install Fail. {exception}");
                    ViewModel.InstallStepText = "安装失败";
                    ViewModel.InstallDetailText = exception.Message;
                    ViewModel.InstallStatus = InstallStatus.Error;
                }
            });

            if (ViewModel.InstallStatus == InstallStatus.Installing)
            {
                ViewModel.InstallStatus = InstallStatus.Finished;
            }
        }
        catch (Exception exception)
        {
            // async void 捕获全部异常
            Debug.WriteLine(exception);
        }
        finally
        {
            InstallStatusControl.IsVisible = false;
            InstallFinishControl.IsVisible = true;
        }
    }

    private void InstallFinishControl_OnOnFinish(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.BeginInvokeShutdown(DispatcherPriority.Default);
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}