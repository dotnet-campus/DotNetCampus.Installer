using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _installCancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        InstallerNavigationControl.NextRequested += async (_, _) => await ShowProgressPageAsync();
        InstallerNavigationControl.CancelRequested += (_, _) => CancelInstallation();
        InstallerNavigationControl.FinishRequested += (_, _) => FinishInstallation();
    }

    private async Task ShowProgressPageAsync()
    {
        if (!PrepareInstallControl.HasAcceptedAgreement)
        {
            return;
        }

        PrepareInstallControl.IsVisible = false;
        InstallProgressControl.IsVisible = true;
        InstallProgressControl.ShowInstallingState();
        InstallerNavigationControl.ShowProgressState();

        _installCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), _installCancellationTokenSource.Token);
            InstallProgressControl.ShowCompletedState();
            InstallerNavigationControl.ShowCompletedState();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _installCancellationTokenSource.Dispose();
            _installCancellationTokenSource = null;
        }
    }

    private void CancelInstallation()
    {
        _installCancellationTokenSource?.Cancel();
        Close();
    }

    private void FinishInstallation()
    {
        if (InstallerNavigationControl.ShouldLaunchApplication)
        {
            // 对接真实安装过程后，在这里启动已安装的应用。
        }

        Close();
    }
}
