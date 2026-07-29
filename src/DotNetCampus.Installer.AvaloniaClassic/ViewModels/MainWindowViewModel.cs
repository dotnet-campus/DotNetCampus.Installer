using DotNetCampus.Installer.AvaloniaClassic.Infrastructure;
using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

using Avalonia.Threading;
using dotnetCampus.Configurations;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DotNetCampus.Installer.AvaloniaClassic.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly ClassicInstallerProgram? _installerProgram;
    private readonly ClassicInstallerConfiguration? _classicInstallerConfiguration;
    private CancellationTokenSource? _installationCancellationTokenSource;
    private bool _closeWhenInstallationStops;
    private InstallerStage _stage = InstallerStage.Preparing;
    private string _installationFolder = @"C:\Program Files\DotNetCampus Installer";
    private bool _hasAcceptedLicense = true;
    private bool _shouldLaunchApplication = true;
    private bool _installationSucceeded;
    private double _installationProgress;
    private string _progressHeadline = "Installing DotNetCampus Installer...";
    private string _currentStepText = "Preparing installation files...";
    private string _progressDetailText = "Waiting for installation to begin.";

    public MainWindowViewModel(ClassicInstallerProgram? installerProgram)
    {
        _installerProgram = installerProgram;
        if (installerProgram is not null)
        {
            _classicInstallerConfiguration = installerProgram.AppConfigurator.Of<ClassicInstallerConfiguration>();
            InitializeFromInstallerProgram(installerProgram, _classicInstallerConfiguration);
        }

        StartInstallationCommand = new AsyncRelayCommand(
            StartInstallationAsync,
            () => HasAcceptedLicense && IsPreparing && !string.IsNullOrWhiteSpace(InstallationFolder));
        CancelInstallationCommand = new RelayCommand(CancelInstallation, () => IsInstalling && !_closeWhenInstallationStops);
        FinishCommand = new RelayCommand(Finish, () => IsCompleted);
        BrowseInstallationFolderCommand = new RelayCommand(() => BrowseInstallationFolderRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? BrowseInstallationFolderRequested;

    public string WindowTitle { get; private set; } = "DotNetCampus Installer Setup";

    public string ProductName { get; private set; } = "DotNetCampus Installer";

    public string WelcomeText { get; private set; } = "Welcome to the\nDotNetCampus Installer Setup";

    public string PreparationHeadline { get; private set; } = "Ready to Install";

    public string PreparationDescription { get; private set; } = "Setup is ready to install DotNetCampus Installer on your computer. Close other applications before continuing, then select Next to begin.";

    public string LicenseText { get; private set; } = "DotNetCampus Installer is a deployment tool for packaging and installing .NET desktop applications. Before continuing, review the selected installation folder and confirm that you accept the applicable license terms. The installer will copy the required files, register application components, and create the selected shortcuts. You can cancel setup while installation is in progress.";

    public string LicenseAgreementText { get; private set; } = "I have read and accept the license agreement";

    public string InstallationFolderLabel { get; private set; } = "Installation Folder";

    /// <summary>
    /// 获取安装目录选择窗口的标题。
    /// </summary>
    public string BrowseInstallationFolderTitle { get; private set; } = "Select Installation Folder";

    public string BrowseButtonText { get; private set; } = "Browse...";

    public string NextButtonText { get; private set; } = "Next >";

    public string CancelButtonText { get; private set; } = "Cancel";

    public string FinishButtonText { get; private set; } = "Finish";

    public string LaunchApplicationText { get; private set; } = "Launch the application when setup finishes";

    /// <summary>
    /// 获取安装完成后是否有可启动的应用程序。
    /// </summary>
    public bool CanLaunchApplication => _installerProgram?.CanLaunchApplication ?? true;

    public bool IsPreparing => _stage == InstallerStage.Preparing;

    public bool IsInstalling => _stage == InstallerStage.Installing;

    public bool IsCompleted => _stage == InstallerStage.Completed;

    public bool ShouldShowLaunchApplicationOption => IsCompleted && _installationSucceeded && CanLaunchApplication;

    public string InstallationFolder
    {
        get => _installationFolder;
        set
        {
            if (!SetProperty(ref _installationFolder, value))
            {
                return;
            }

            if (_installerProgram is not null && !string.IsNullOrWhiteSpace(value))
            {
                var context = _installerProgram.StandardInstallContext;
                context.InstallRootPath = value;
                context.MainInstallPath = Path.Join(value, $"{context.ProductName}_{context.AppVersion}");
            }

            StartInstallationCommand.NotifyCanExecuteChanged();
        }
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
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        InstallationFolder = path;
    }

    private void InitializeFromInstallerProgram(
        ClassicInstallerProgram installerProgram,
        ClassicInstallerConfiguration configuration)
    {
        var context = installerProgram.StandardInstallContext;
        var productName = context.DisplayProductName;

        ProductName = productName;
        WindowTitle = GetConfiguredText(configuration.WindowTitle, $"{productName} Setup");
        WelcomeText = GetConfiguredText(configuration.WelcomeText, $"Welcome to the\n{productName} Setup");
        PreparationHeadline = GetConfiguredText(configuration.PreparationHeadline, "Ready to Install");
        PreparationDescription = GetConfiguredText(
            configuration.PreparationDescription,
            $"Setup is ready to install {productName} on your computer. Close other applications before continuing, then select Next to begin.");
        LicenseText = GetConfiguredText(
            configuration.LicenseText,
            $"Before installing {productName}, review the selected installation folder and confirm that you accept the applicable license terms. Setup will copy the required files, register application components, and create the available shortcuts. You can cancel setup while installation is in progress.");
        LicenseAgreementText = GetConfiguredText(configuration.LicenseAgreementText, "I have read and accept the license agreement");
        InstallationFolderLabel = GetConfiguredText(configuration.InstallationFolderLabel, "Installation Folder");
        BrowseInstallationFolderTitle = GetConfiguredText(configuration.BrowseInstallationFolderTitle, "Select Installation Folder");
        BrowseButtonText = GetConfiguredText(configuration.BrowseButtonText, "Browse...");
        NextButtonText = GetConfiguredText(configuration.NextButtonText, "Next >");
        CancelButtonText = GetConfiguredText(configuration.CancelButtonText, "Cancel");
        FinishButtonText = GetConfiguredText(configuration.FinishButtonText, "Finish");
        LaunchApplicationText = GetConfiguredText(configuration.LaunchApplicationText, $"Launch {productName} when setup finishes");

        _installationFolder = context.InstallRootPath;
        _hasAcceptedLicense = configuration.HasAcceptedLicenseByDefault ?? false;
        _shouldLaunchApplication = installerProgram.CanLaunchApplication
            && (configuration.ShouldLaunchApplicationByDefault ?? true);
        _progressHeadline = GetConfiguredText(configuration.InstallingHeadline, $"Installing {productName}...");
        _currentStepText = GetConfiguredText(configuration.PreparingInstallationStepText, "Preparing installation files...");
        _progressDetailText = GetConfiguredText(configuration.PreparingInstallationDetailText, "Waiting for installation to begin.");
    }

    private static string GetConfiguredText(string? configuredText, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(configuredText) ? fallbackText : configuredText;
    }

    private async Task StartInstallationAsync()
    {
        _closeWhenInstallationStops = false;
        _installationSucceeded = false;
        OnPropertyChanged(nameof(ShouldShowLaunchApplicationOption));
        SetStage(InstallerStage.Installing);
        using var cancellationTokenSource = new CancellationTokenSource();
        _installationCancellationTokenSource = cancellationTokenSource;

        if (_installerProgram is not null)
        {
            _installerProgram.ProgressChanged -= InstallerProgram_ProgressChanged;
            _installerProgram.ProgressChanged += InstallerProgram_ProgressChanged;
        }

        try
        {
            if (_installerProgram is null)
            {
                await ShowSimulatedInstallationAsync(cancellationTokenSource.Token);
            }
            else
            {
                await Task.Run(
                    () => _installerProgram.InstallAsync(cancellationTokenSource.Token),
                    cancellationTokenSource.Token);
            }

            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            _installationSucceeded = true;
            ShowCompletedState();
            SetStage(InstallerStage.Completed);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _installerProgram?.Logger.WriteLog($"[Error] Install failed. {exception}");
            if (!_closeWhenInstallationStops)
            {
                ShowFailedState();
                SetStage(InstallerStage.Completed);
            }
        }
        finally
        {
            if (_installerProgram is not null)
            {
                _installerProgram.ProgressChanged -= InstallerProgram_ProgressChanged;
            }

            if (ReferenceEquals(_installationCancellationTokenSource, cancellationTokenSource))
            {
                _installationCancellationTokenSource = null;
            }

            if (_closeWhenInstallationStops)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void CancelInstallation()
    {
        _closeWhenInstallationStops = true;
        _installationCancellationTokenSource?.Cancel();
        CancelInstallationCommand.NotifyCanExecuteChanged();
    }

    private void Finish()
    {
        if (_installationSucceeded && ShouldLaunchApplication && _installerProgram?.CanLaunchApplication is true)
        {
            try
            {
                _installerProgram.TryLaunchApplication();
            }
            catch (Exception exception)
            {
                _installerProgram.Logger.WriteLog($"[Error] Launch application failed. {exception}");
            }
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void InstallerProgram_ProgressChanged(object? sender, ClassicInstallerProgressChangedEventArgs e)
    {
        void UpdateProgress()
        {
            InstallationProgress = e.ProgressPercentage;
            var (stepText, detailText) = GetProgressText(e);
            CurrentStepText = stepText;
            ProgressDetailText = detailText;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateProgress();
        }
        else
        {
            _ = Dispatcher.UIThread.InvokeAsync(UpdateProgress, DispatcherPriority.Send);
        }
    }

    private (string StepText, string DetailText) GetProgressText(ClassicInstallerProgressChangedEventArgs e)
    {
        var configuration = _classicInstallerConfiguration;
        return e.Stage switch
        {
            ClassicInstallerProgressStage.PreparingInstallation =>
                (GetConfiguredText(configuration?.PreparingInstallationStepText, "Preparing installation..."),
                    GetConfiguredText(configuration?.PreparingInstallationDetailText, "Initializing installation settings.")),
            ClassicInstallerProgressStage.CheckingExistingInstallation =>
                (GetConfiguredText(configuration?.CheckingExistingInstallationStepText, "Checking existing installation..."),
                    GetConfiguredText(configuration?.CheckingExistingInstallationDetailText, "Looking for files from an earlier version.")),
            ClassicInstallerProgressStage.DeployingApplicationFiles =>
                (GetConfiguredText(configuration?.DeployingApplicationFilesStepText, "Deploying application files..."),
                    GetConfiguredText(configuration?.DeployingApplicationFilesDetailText, GetDeployingDetailText(e.CurrentFileName))),
            ClassicInstallerProgressStage.RegisteringApplication =>
                (GetConfiguredText(configuration?.RegisteringApplicationStepText, "Registering application..."),
                    GetConfiguredText(configuration?.RegisteringApplicationDetailText, "Writing application information to the system.")),
            ClassicInstallerProgressStage.CreatingShortcuts =>
                (GetConfiguredText(configuration?.CreatingShortcutsStepText, "Creating shortcuts..."),
                    GetConfiguredText(configuration?.CreatingShortcutsDetailText, "Creating application shortcuts.")),
            ClassicInstallerProgressStage.Completed =>
                (GetConfiguredText(configuration?.InstallationSucceededStepText, $"{ProductName} was installed successfully."),
                    GetConfiguredText(configuration?.InstallationCompleteDetailText, "Select Finish to close the setup wizard.")),
            _ => (CurrentStepText, ProgressDetailText)
        };
    }

    private string GetDeployingDetailText(string? currentFileName)
    {
        return string.IsNullOrWhiteSpace(currentFileName)
            ? $"Writing files to {InstallationFolder}."
            : $"Writing {currentFileName} to {InstallationFolder}.";
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
        var configuration = _classicInstallerConfiguration;
        ProgressHeadline = GetConfiguredText(configuration?.InstallationCompleteHeadline, "Installation Complete");
        InstallationProgress = 100;
        CurrentStepText = GetConfiguredText(
            configuration?.InstallationSucceededStepText,
            $"{ProductName} was installed successfully.");
        ProgressDetailText = GetConfiguredText(
            configuration?.InstallationCompleteDetailText,
            "Select Finish to close the setup wizard.");
    }

    private void ShowFailedState()
    {
        var configuration = _classicInstallerConfiguration;
        ProgressHeadline = GetConfiguredText(configuration?.InstallationFailedHeadline, "Installation Failed");
        CurrentStepText = GetConfiguredText(configuration?.InstallationFailedStepText, $"{ProductName} could not be installed.");
        ProgressDetailText = GetConfiguredText(
            configuration?.InstallationFailedDetailText,
            "Setup encountered an unexpected error. See the installation log for details.");
    }

    private void SetStage(InstallerStage stage)
    {
        _stage = stage;
        OnPropertyChanged(nameof(IsPreparing));
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(ShouldShowLaunchApplicationOption));
        StartInstallationCommand.NotifyCanExecuteChanged();
        CancelInstallationCommand.NotifyCanExecuteChanged();
        FinishCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        if (_installerProgram is not null)
        {
            _installerProgram.ProgressChanged -= InstallerProgram_ProgressChanged;
        }

        _installationCancellationTokenSource?.Cancel();
    }

    private enum InstallerStage
    {
        Preparing,
        Installing,
        Completed
    }
}
