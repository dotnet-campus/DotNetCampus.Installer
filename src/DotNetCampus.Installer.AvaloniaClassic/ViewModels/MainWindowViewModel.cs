using DotNetCampus.Installer.AvaloniaClassic.Infrastructure;
using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DotNetCampus.Installer.AvaloniaClassic.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private CancellationTokenSource? _installationCancellationTokenSource;
    private InstallerStage _stage = InstallerStage.Preparing;
    private string _installationFolder = @"C:\Program Files\DotNetCampus Installer";
    private bool _hasAcceptedLicense = true;
    private bool _shouldLaunchApplication = true;
    private double _installationProgress;
    private string _progressHeadline = "Installing DotNetCampus Installer...";
    private string _currentStepText = "Preparing installation files...";
    private string _progressDetailText = "Waiting for installation to begin.";

    public MainWindowViewModel(ClassicInstallerProgram? installerProgram)
    {
        StartInstallationCommand = new AsyncRelayCommand(StartInstallationAsync, () => HasAcceptedLicense && IsPreparing);
        CancelInstallationCommand = new RelayCommand(CancelInstallation, () => IsInstalling);
        FinishCommand = new RelayCommand(Finish, () => IsCompleted);
        BrowseInstallationFolderCommand = new RelayCommand(() => BrowseInstallationFolderRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? BrowseInstallationFolderRequested;

    public string WindowTitle => "DotNetCampus Installer Setup";

    public string ProductName => "DotNetCampus Installer";

    public string WelcomeText => "Welcome to the\nDotNetCampus Installer Setup";

    public string PreparationHeadline => "Ready to Install";

    public string PreparationDescription => "Setup is ready to install DotNetCampus Installer on your computer. Close other applications before continuing, then select Next to begin.";

    public string LicenseText => "DotNetCampus Installer is a deployment tool for packaging and installing .NET desktop applications. Before continuing, review the selected installation folder and confirm that you accept the applicable license terms. The installer will copy the required files, register application components, and create the selected shortcuts. You can cancel setup while installation is in progress.";

    public string LicenseAgreementText => "I have read and accept the license agreement";

    public string InstallationFolderLabel => "Installation Folder";

    public string BrowseButtonText => "Browse...";

    public string NextButtonText => "Next >";

    public string CancelButtonText => "Cancel";

    public string FinishButtonText => "Finish";

    public string LaunchApplicationText => "Launch the application when setup finishes";

    public bool IsPreparing => _stage == InstallerStage.Preparing;

    public bool IsInstalling => _stage == InstallerStage.Installing;

    public bool IsCompleted => _stage == InstallerStage.Completed;

    public string InstallationFolder
    {
        get => _installationFolder;
        set => SetProperty(ref _installationFolder, value);
    }

    public bool HasAcceptedLicense
    {
        get => _hasAcceptedLicense;
        set
        {
            if (SetProperty(ref _hasAcceptedLicense, value))
            {
                StartInstallationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool ShouldLaunchApplication
    {
        get => _shouldLaunchApplication;
        set => SetProperty(ref _shouldLaunchApplication, value);
    }

    public double InstallationProgress
    {
        get => _installationProgress;
        private set => SetProperty(ref _installationProgress, value);
    }

    public string ProgressHeadline
    {
        get => _progressHeadline;
        private set => SetProperty(ref _progressHeadline, value);
    }

    public string CurrentStepText
    {
        get => _currentStepText;
        private set => SetProperty(ref _currentStepText, value);
    }

    public string ProgressDetailText
    {
        get => _progressDetailText;
        private set => SetProperty(ref _progressDetailText, value);
    }

    public AsyncRelayCommand StartInstallationCommand { get; }

    public RelayCommand CancelInstallationCommand { get; }

    public RelayCommand FinishCommand { get; }

    public ICommand BrowseInstallationFolderCommand { get; }

    public void SetInstallationFolder(string path)
    {
        InstallationFolder = path;
    }

    private async Task StartInstallationAsync()
    {
        SetStage(InstallerStage.Installing);
        using var cancellationTokenSource = new CancellationTokenSource();
        _installationCancellationTokenSource = cancellationTokenSource;

        try
        {
            await ShowSimulatedInstallationAsync(cancellationTokenSource.Token);
            ShowCompletedState();
            SetStage(InstallerStage.Completed);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_installationCancellationTokenSource, cancellationTokenSource))
            {
                _installationCancellationTokenSource = null;
            }
        }
    }

    private void CancelInstallation()
    {
        _installationCancellationTokenSource?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Finish()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ShowSimulatedInstallationAsync(CancellationToken cancellationToken)
    {
        ProgressHeadline = "Installing DotNetCampus Installer...";
        await ShowSimulatedStepAsync(8, "Preparing installation...", "Initializing installation settings.", cancellationToken);
        await ShowSimulatedStepAsync(20, "Checking existing installation...", "Looking for files from an earlier version.", cancellationToken);
        await ShowSimulatedStepAsync(64, "Deploying application files...", $"Writing files to {InstallationFolder}.", cancellationToken);
        await ShowSimulatedStepAsync(90, "Registering application...", "Writing application information to the system.", cancellationToken);
        await ShowSimulatedStepAsync(98, "Creating shortcuts...", "Creating application shortcuts.", cancellationToken);
    }

    private async Task ShowSimulatedStepAsync(double progress, string stepText, string detailText, CancellationToken cancellationToken)
    {
        InstallationProgress = progress;
        CurrentStepText = stepText;
        ProgressDetailText = detailText;
        await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
    }

    private void ShowCompletedState()
    {
        ProgressHeadline = "Installation Complete";
        InstallationProgress = 100;
        CurrentStepText = "DotNetCampus Installer was installed successfully.";
        ProgressDetailText = "Select Finish to close the setup wizard.";
    }

    private void SetStage(InstallerStage stage)
    {
        _stage = stage;
        OnPropertyChanged(nameof(IsPreparing));
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(IsCompleted));
        StartInstallationCommand.NotifyCanExecuteChanged();
        CancelInstallationCommand.NotifyCanExecuteChanged();
        FinishCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _installationCancellationTokenSource?.Cancel();
        _installationCancellationTokenSource?.Dispose();
    }

    private enum InstallerStage
    {
        Preparing,
        Installing,
        Completed
    }
}
