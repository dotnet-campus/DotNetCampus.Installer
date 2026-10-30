using System;
using System.Diagnostics;
using System.IO;
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
            // 安装过程包含同步 LZMA 解压，必须在后台线程执行。
            await Task.Run(InstallerProgram.InstallAsync);

            if (ViewModel.InstallStatus == InstallStatus.Installing)
            {
                ViewModel.InstallStatus = InstallStatus.Finished;
            }
        }
        catch (Exception exception)
        {
            // await 会回到 UI 线程，在这里统一更新界面状态。
            try
            {
                InstallerProgram.Logger.WriteLog($"[Error] Install Fail. {exception}");
            }
            catch (IOException loggingException)
            {
                // 安装失败状态必须优先展示，不能让日志文件错误中断 UI 更新。
                Trace.WriteLine(loggingException);
            }
            catch (UnauthorizedAccessException loggingException)
            {
                // 安装失败状态必须优先展示，不能让日志权限错误中断 UI 更新。
                Trace.WriteLine(loggingException);
            }

            ViewModel.InstallStepText = "安装失败";
            ViewModel.InstallDetailText = exception.Message;
            ViewModel.InstallStatus = InstallStatus.Error;
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