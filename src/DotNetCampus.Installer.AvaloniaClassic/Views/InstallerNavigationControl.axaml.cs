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

    public event EventHandler? CancelRequested;

    public event EventHandler? FinishRequested;

    public bool ShouldLaunchApplication => LaunchApplicationCheckBox.IsChecked == true;

    public void ShowPrepareState()
    {
        InstallationFolderLabel.IsVisible = true;
        InstallationFolderTextBox.IsVisible = true;
        BrowseButton.IsVisible = true;
        LaunchApplicationPanel.IsVisible = false;
        NextButton.IsVisible = true;
        CancelButton.IsVisible = false;
        FinishButton.IsVisible = false;
    }

    public void ShowProgressState()
    {
        InstallationFolderLabel.IsVisible = false;
        InstallationFolderTextBox.IsVisible = false;
        BrowseButton.IsVisible = false;
        LaunchApplicationPanel.IsVisible = false;
        NextButton.IsVisible = false;
        CancelButton.IsVisible = true;
        FinishButton.IsVisible = false;
    }

    public void ShowCompletedState()
    {
        InstallationFolderLabel.IsVisible = false;
        InstallationFolderTextBox.IsVisible = false;
        BrowseButton.IsVisible = false;
        LaunchApplicationPanel.IsVisible = true;
        NextButton.IsVisible = false;
        CancelButton.IsVisible = false;
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

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void FinishButton_OnClick(object? sender, RoutedEventArgs e) => FinishRequested?.Invoke(this, EventArgs.Empty);
}
