using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace DotNetCampus.Installer.AvaloniaClassic.Views;

public partial class InstallerNavigationControl : UserControl
{
    public InstallerNavigationControl()
    {
        InitializeComponent();
    }

    public event EventHandler? NextRequested;

    public event EventHandler? BackRequested;

    public event EventHandler? CancelRequested;

    public event EventHandler? FinishRequested;

    public void ShowPrepareState()
    {
        InstallationFolderPanel.IsVisible = true;
        BackButton.IsVisible = false;
        NextButton.IsVisible = true;
        FinishButton.IsVisible = false;
    }

    public void ShowProgressState()
    {
        InstallationFolderPanel.IsVisible = false;
        BackButton.IsVisible = false;
        NextButton.IsVisible = false;
        FinishButton.IsVisible = true;
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Installation Folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is not null)
        {
            InstallationFolderTextBox.Text = folder.Path.LocalPath;
        }
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);

    private void BackButton_OnClick(object? sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void FinishButton_OnClick(object? sender, RoutedEventArgs e) => FinishRequested?.Invoke(this, EventArgs.Empty);
}
