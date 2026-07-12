using Avalonia.Controls;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        InstallerNavigationControl.NextRequested += (_, _) => ShowProgressPage();
        InstallerNavigationControl.BackRequested += (_, _) => ShowPreparePage();
        InstallerNavigationControl.CancelRequested += (_, _) => Close();
        InstallerNavigationControl.FinishRequested += (_, _) => Close();
    }

    private void ShowProgressPage()
    {
        if (!PrepareInstallControl.HasAcceptedAgreement)
        {
            return;
        }

        PrepareInstallControl.IsVisible = false;
        InstallProgressControl.IsVisible = true;
        InstallerNavigationControl.ShowProgressState();
    }

    private void ShowPreparePage()
    {
        PrepareInstallControl.IsVisible = true;
        InstallProgressControl.IsVisible = false;
        InstallerNavigationControl.ShowPrepareState();
    }
}
