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
    private string _progressHeadline = LocalizedText.Current.InstallingHeadline.ToString("DotNetCampus Installer");
    private string _currentStepText = LocalizedText.Current.PreparingFilesText;
    private string _progressDetailText = LocalizedText.Current.WaitingText;

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

    public string WindowTitle { get; private set; } = LocalizedText.Current.WindowTitle.ToString("DotNetCampus Installer");

    public string ProductName { get; private set; } = "DotNetCampus Installer";

    public string WelcomeText { get; private set; } = LocalizedText.Current.WelcomeText.ToString("DotNetCampus Installer");

    public string PreparationHeadline { get; private set; } = LocalizedText.Current.PreparationHeadline;

    public string PreparationDescription { get; private set; } = LocalizedText.Current.PreparationDescription.ToString("DotNetCampus Installer");

    public string LicenseText { get; private set; } = LocalizedText.Current.LicenseText.ToString("DotNetCampus Installer");

    public string LicenseAgreementText { get; private set; } = LocalizedText.Current.LicenseAgreementText;

    public string InstallationFolderLabel { get; private set; } = LocalizedText.Current.InstallationFolderLabel;

    /// <summary>
    /// 获取安装目录选择窗口的标题。
    /// </summary>
    public string BrowseInstallationFolderTitle { get; private set; } = LocalizedText.Current.BrowseInstallationFolderTitle;

    public string BrowseButtonText { get; private set; } = LocalizedText.Current.BrowseButtonText;

    public string NextButtonText { get; private set; } = LocalizedText.Current.NextButtonText;

    public string CancelButtonText { get; private set; } = LocalizedText.Current.CancelButtonText;

    public string FinishButtonText { get; private set; } = LocalizedText.Current.FinishButtonText;

    public string LaunchApplicationText { get; private set; } = LocalizedText.Current.LaunchApplicationText.ToString("DotNetCampus Installer");

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
        var text = LocalizedText.Current;
        WindowTitle = GetConfiguredText(configuration.WindowTitle, text.WindowTitle.ToString(productName));
        WelcomeText = GetConfiguredText(configuration.WelcomeText, text.WelcomeText.ToString(productName));
        PreparationHeadline = GetConfiguredText(configuration.PreparationHeadline, text.PreparationHeadline);
        PreparationDescription = GetConfiguredText(configuration.PreparationDescription, text.PreparationDescription.ToString(productName));
        LicenseText = GetConfiguredText(configuration.LicenseText, text.LicenseText.ToString(productName));
        LicenseAgreementText = GetConfiguredText(configuration.LicenseAgreementText, text.LicenseAgreementText);
        InstallationFolderLabel = GetConfiguredText(configuration.InstallationFolderLabel, text.InstallationFolderLabel);
        BrowseInstallationFolderTitle = GetConfiguredText(configuration.BrowseInstallationFolderTitle, text.BrowseInstallationFolderTitle);
        BrowseButtonText = GetConfiguredText(configuration.BrowseButtonText, text.BrowseButtonText);
        NextButtonText = GetConfiguredText(configuration.NextButtonText, text.NextButtonText);
        CancelButtonText = GetConfiguredText(configuration.CancelButtonText, text.CancelButtonText);
        FinishButtonText = GetConfiguredText(configuration.FinishButtonText, text.FinishButtonText);
        LaunchApplicationText = GetConfiguredText(configuration.LaunchApplicationText, text.LaunchApplicationText.ToString(productName));

        _installationFolder = context.InstallRootPath;
        _hasAcceptedLicense = configuration.HasAcceptedLicenseByDefault ?? false;
        _shouldLaunchApplication = installerProgram.CanLaunchApplication
            && (configuration.ShouldLaunchApplicationByDefault ?? true);
        _progressHeadline = GetConfiguredText(configuration.InstallingHeadline, text.InstallingHeadline.ToString(productName));
        _currentStepText = GetConfiguredText(configuration.PreparingInstallationStepText, text.PreparingFilesText);
        _progressDetailText = GetConfiguredText(configuration.PreparingInstallationDetailText, text.WaitingText);
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
        var text = LocalizedText.Current;
        return e.Stage switch
        {
            ClassicInstallerProgressStage.PreparingInstallation =>
                (GetConfiguredText(configuration?.PreparingInstallationStepText, text.PreparingInstallationStepText),
                    GetConfiguredText(configuration?.PreparingInstallationDetailText, text.PreparingInstallationDetailText)),
            ClassicInstallerProgressStage.CheckingExistingInstallation =>
                (GetConfiguredText(configuration?.CheckingExistingInstallationStepText, text.CheckingExistingInstallationStepText),
                    GetConfiguredText(configuration?.CheckingExistingInstallationDetailText, text.CheckingExistingInstallationDetailText)),
            ClassicInstallerProgressStage.DeployingApplicationFiles =>
                (GetConfiguredText(configuration?.DeployingApplicationFilesStepText, text.DeployingApplicationFilesStepText),
                    GetConfiguredText(configuration?.DeployingApplicationFilesDetailText, GetDeployingDetailText(e.CurrentFileName))),
            ClassicInstallerProgressStage.RegisteringApplication =>
                (GetConfiguredText(configuration?.RegisteringApplicationStepText, text.RegisteringApplicationStepText),
                    GetConfiguredText(configuration?.RegisteringApplicationDetailText, text.RegisteringApplicationDetailText)),
            ClassicInstallerProgressStage.CreatingShortcuts =>
                (GetConfiguredText(configuration?.CreatingShortcutsStepText, text.CreatingShortcutsStepText),
                    GetConfiguredText(configuration?.CreatingShortcutsDetailText, text.CreatingShortcutsDetailText)),
            ClassicInstallerProgressStage.Completed =>
                (GetConfiguredText(configuration?.InstallationSucceededStepText, text.InstallationSucceededStepText.ToString(ProductName)),
                    GetConfiguredText(configuration?.InstallationCompleteDetailText, text.InstallationCompleteDetailText)),
            _ => (CurrentStepText, ProgressDetailText)
        };
    }

    private string GetDeployingDetailText(string? currentFileName)
    {
        return string.IsNullOrWhiteSpace(currentFileName)
            ? LocalizedText.Current.WritingFilesText.ToString(InstallationFolder)
            : LocalizedText.Current.WritingFileText.ToString(currentFileName, InstallationFolder);
    }

    private async Task ShowSimulatedInstallationAsync(CancellationToken cancellationToken)
    {
        var text = LocalizedText.Current;
        ProgressHeadline = text.InstallingHeadline.ToString(ProductName);
        await ShowSimulatedStepAsync(8, text.PreparingInstallationStepText, text.PreparingInstallationDetailText, cancellationToken);
        await ShowSimulatedStepAsync(20, text.CheckingExistingInstallationStepText, text.CheckingExistingInstallationDetailText, cancellationToken);
        await ShowSimulatedStepAsync(64, text.DeployingApplicationFilesStepText, text.WritingFilesText.ToString(InstallationFolder), cancellationToken);
        await ShowSimulatedStepAsync(90, text.RegisteringApplicationStepText, text.RegisteringApplicationDetailText, cancellationToken);
        await ShowSimulatedStepAsync(98, text.CreatingShortcutsStepText, text.CreatingShortcutsDetailText, cancellationToken);
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
        ProgressHeadline = GetConfiguredText(configuration?.InstallationCompleteHeadline, LocalizedText.Current.InstallationCompleteHeadline);
        InstallationProgress = 100;
        CurrentStepText = GetConfiguredText(
            configuration?.InstallationSucceededStepText,
            LocalizedText.Current.InstallationSucceededStepText.ToString(ProductName));
        ProgressDetailText = GetConfiguredText(
            configuration?.InstallationCompleteDetailText,
            LocalizedText.Current.InstallationCompleteDetailText);
    }

    private void ShowFailedState()
    {
        var configuration = _classicInstallerConfiguration;
        ProgressHeadline = GetConfiguredText(configuration?.InstallationFailedHeadline, LocalizedText.Current.InstallationFailedHeadline);
        CurrentStepText = GetConfiguredText(configuration?.InstallationFailedStepText, LocalizedText.Current.InstallationFailedStepText.ToString(ProductName));
        ProgressDetailText = GetConfiguredText(
            configuration?.InstallationFailedDetailText,
            LocalizedText.Current.InstallationFailedDetailText);
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
