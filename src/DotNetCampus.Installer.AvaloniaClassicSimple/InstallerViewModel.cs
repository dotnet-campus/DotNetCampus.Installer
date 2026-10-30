using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using dotnetCampus.Configurations;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassicSimple;

/// <summary>简洁安装界面的页面状态。</summary>
public enum InstallerPage { License, Options, Progress, Finished }

/// <summary>不依赖窗口的安装交互状态。</summary>
public sealed class InstallerViewModel : INotifyPropertyChanged
{
    private InstallerPage _page;
    private bool _accepted;
    private string _draftPath;
    private double _progress;
    private string _status = LocalizedText.Current.Preparing;
    private string _result = "";
    private string _error = "";

    /// <summary>从产品信息创建界面状态。</summary>
    public InstallerViewModel(string productName, string installationPath, string? licenseText = null, IImage? productIcon = null)
    {
        ProductName = productName;
        InstallationPath = installationPath;
        _draftPath = installationPath;
        LicenseText = licenseText ?? LocalizedText.Current.License.ToString(productName);
        ProductIcon = productIcon;
        WindowTitle = LocalizedText.Current.WindowTitle.ToString(productName);
    }

    /// <summary>读取现有安装包的产品信息和 Classic 配置。</summary>
    public static InstallerViewModel FromProgram(ClassicInstallerProgram program, IImage? icon = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        var config = program.AppConfigurator.Of<ClassicInstallerConfiguration>();
        return new InstallerViewModel(program.StandardInstallContext.DisplayProductName,
            program.StandardInstallContext.InstallRootPath, config.LicenseText, icon)
        {
            WindowTitle = config.WindowTitle ?? LocalizedText.Current.WindowTitle.ToString(program.StandardInstallContext.DisplayProductName)
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string ProductName { get; }
    public IImage? ProductIcon { get; }
    public string LicenseText { get; }
    public string WindowTitle { get; private init; }
    public InstallerPage Page => _page;
    public bool IsLicense => Page == InstallerPage.License;
    public bool IsOptions => Page == InstallerPage.Options;
    public bool IsProgress => Page == InstallerPage.Progress;
    public bool IsFinished => Page == InstallerPage.Finished;
    public bool CanInstall => IsLicense && HasAcceptedLicense;
    public bool HasAcceptedLicense { get => _accepted; set { _accepted = value; Changed(); Changed(nameof(CanInstall)); } }
    public string InstallationPath { get; private set; }
    public string DraftPath { get => _draftPath; set { _draftPath = value; Changed(); } }
    public double Progress => _progress;
    public string Status => _status;
    public string Result => _result;
    public string Error => _error;
    public string AgreementText => LocalizedText.Current.Agreement;
    public string OptionsText => LocalizedText.Current.Options;
    public string InstallText => LocalizedText.Current.Install;
    public string CancelText => LocalizedText.Current.Cancel;
    public string CloseText => LocalizedText.Current.Close;
    public string OkText => LocalizedText.Current.Ok;
    public string BrowseText => LocalizedText.Current.Browse;
    public string OptionsTitle => LocalizedText.Current.OptionsTitle;
    public string LocationText => LocalizedText.Current.Location;
    public string ProgressTitle => LocalizedText.Current.ProgressTitle;
    public string ProcessingText => LocalizedText.Current.Processing;

    /// <summary>开始编辑安装目录，不立即修改已提交路径。</summary>
    public void OpenOptions() { if (!IsLicense) return; DraftPath = InstallationPath; SetError(""); SetPage(InstallerPage.Options); }

    /// <summary>验证并提交安装目录。</summary>
    public bool CommitOptions()
    {
        if (!IsOptions) return false;
        try
        {
            if (string.IsNullOrWhiteSpace(DraftPath) || !Path.IsPathFullyQualified(DraftPath))
            { SetError(LocalizedText.Current.InvalidPath); return false; }
            InstallationPath = Path.GetFullPath(DraftPath);
        }
        catch (ArgumentException) { SetError(LocalizedText.Current.InvalidPath); return false; }
        catch (NotSupportedException) { SetError(LocalizedText.Current.InvalidPath); return false; }
        Changed(nameof(InstallationPath)); SetError(""); SetPage(InstallerPage.License); return true;
    }

    /// <summary>放弃尚未提交的目录编辑。</summary>
    public void CancelOptions() { if (!IsOptions) return; DraftPath = InstallationPath; SetError(""); SetPage(InstallerPage.License); }

    /// <summary>在同意协议后切换到安装进度页。</summary>
    public bool BeginInstallation() { if (!CanInstall) return false; SetPage(InstallerPage.Progress); return true; }

    /// <summary>显示安装内核报告的进度。</summary>
    public void ReportProgress(ClassicInstallerProgressChangedEventArgs progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _progress = progress.ProgressPercentage;
        var text = LocalizedText.Current;
        _status = progress.CurrentFileName ?? progress.Stage switch
        {
            ClassicInstallerProgressStage.PreparingInstallation => text.Preparing,
            ClassicInstallerProgressStage.CheckingExistingInstallation => text.Checking,
            ClassicInstallerProgressStage.DeployingApplicationFiles => text.Deploying,
            ClassicInstallerProgressStage.RegisteringApplication => text.Registering,
            ClassicInstallerProgressStage.CreatingShortcuts => text.Shortcuts,
            _ => text.Success
        };
        Changed(nameof(Progress)); Changed(nameof(Status));
    }

    /// <summary>显示安装成功、失败或取消的结果。</summary>
    public void CompleteInstallation(string? error = null, bool cancelled = false)
    {
        _result = cancelled ? LocalizedText.Current.Cancelled : error is null ? LocalizedText.Current.Success : LocalizedText.Current.Failure;
        SetError(error ?? ""); Changed(nameof(Result)); SetPage(InstallerPage.Finished);
    }

    private void SetError(string error) { _error = error; Changed(nameof(Error)); }
    private void SetPage(InstallerPage page)
    {
        _page = page;
        foreach (var name in new[] { nameof(Page), nameof(IsLicense), nameof(IsOptions), nameof(IsProgress), nameof(IsFinished), nameof(CanInstall) }) Changed(name);
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
