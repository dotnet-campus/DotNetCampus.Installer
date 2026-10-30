using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DotNetCampus.Installer.AvaloniaSample.ViewModels;

namespace DotNetCampus.Installer.AvaloniaSample.Views;

/// <summary>
/// 安装条
/// </summary>
public partial class InstallBarUserControl : UserControl
{
    public InstallBarUserControl()
    {
        InitializeComponent();
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext!;

    private void InstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 快速安装
        RequestInstallStart?.Invoke(this, EventArgs.Empty);
    }

    private void CustomInstallPathButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 自定义安装
        MainInstallGrid.IsVisible = false;
        SelectInstallPathGrid.IsVisible = true;
    }

    private async void PickFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 安装路径
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            var installPath = await window.StorageProvider.TryGetFolderFromPathAsync(ViewModel.InstallPath);

            var folderList = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                SuggestedStartLocation = installPath,
                AllowMultiple = false,
                Title = "选择安装路径",
                SuggestedFileName = ViewModel.StandardInstallContext.ProductName
            });

            if (folderList.Count == 1)
            {
                var folder = folderList[0].TryGetLocalPath();
                if (folder is not null)
                {
                     ViewModel.InstallPath = folder;
                }
            }
        }
    }

    private void BackToMainInstallGridButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MainInstallGrid.IsVisible = true;
        SelectInstallPathGrid.IsVisible = false;
    }

    private void StartInstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 开始安装
        RequestInstallStart?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 准备开始安装时请求触发的事件
    /// </summary>
    public event EventHandler? RequestInstallStart;
}