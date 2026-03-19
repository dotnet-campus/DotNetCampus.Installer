using Avalonia.Controls;
using Avalonia.Interactivity;

using System;

namespace DotNetCampus.Installer.AvaloniaSample.Views;

public partial class InstallFinishUserControl : UserControl
{
    public InstallFinishUserControl()
    {
        InitializeComponent();
    }

    private void FinishInstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OnFinish?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnFinish;
}
