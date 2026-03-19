using Avalonia.Threading;
using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace DotNetCampus.Installer.AvaloniaSample.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
        InstallerProgram.ProgressChanged += InstallerProgram_ProgressChanged;

        _installStepText = "准备安装环境";
        _installDetailText = $"将安装到 {InstallPath}";
        _installProgressText = "等待开始";
    }

    /// <summary>
    /// 安装状态
    /// </summary>
    public InstallStatus InstallStatus
    {
        get => _installStatus;
        set
        {
            if (value == _installStatus) return;
            _installStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReadyToInstall));
            OnPropertyChanged(nameof(IsInstalling));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(InstallHeadline));
            OnPropertyChanged(nameof(InstallDescription));
            OnPropertyChanged(nameof(FinishButtonText));
        }
    }
    private InstallStatus _installStatus = InstallStatus.Ready;

    public string ProductName => StandardInstallContext.DisplayProductName;

    public string ProductFamily => StandardInstallContext.DisplayProductFamily;

    public string AppVersion => StandardInstallContext.AppVersion;

    public bool IsReadyToInstall => InstallStatus == InstallStatus.Ready;

    public bool IsInstalling => InstallStatus == InstallStatus.Installing;

    public bool IsCompleted => InstallStatus is InstallStatus.Finished or InstallStatus.Error;

    public string InstallHeadline => InstallStatus switch
    {
        InstallStatus.Ready => $"准备安装 {ProductName}",
        InstallStatus.Installing => "正在安装应用",
        InstallStatus.Finished => "安装已完成",
        InstallStatus.Error => "安装未完成",
        _ => ProductName,
    };

    public string InstallDescription => InstallStatus switch
    {
        InstallStatus.Ready => $"已为你准备好安装目录和核心组件，点击开始即可完成部署。",
        InstallStatus.Installing => InstallDetailText,
        InstallStatus.Finished => $"{ProductName} 已成功部署到本机，可以立即结束安装向导。",
        InstallStatus.Error => "安装过程中发生异常，请查看安装日志并重试。",
        _ => string.Empty,
    };

    public string FinishButtonText => InstallStatus == InstallStatus.Error ? "关闭安装器" : "完成安装";

    /// <inheritdoc cref="DotNetCampus.Installer.Lib.StandardInstallerPrograms.StandardInstallContext.InstallRootPath"/>
    public string InstallPath
    {
        get => StandardInstallContext.InstallRootPath;
        set
        {
            if (value == StandardInstallContext.InstallRootPath) return;
            StandardInstallContext.InstallRootPath = value;
            StandardInstallContext.MainInstallPath = Path.Join(value, $"{StandardInstallContext.ProductName}_{StandardInstallContext.AppVersion}");
            OnPropertyChanged();
            OnPropertyChanged(nameof(InstallLocationHint));

            if (InstallStatus == InstallStatus.Ready)
            {
                InstallDetailText = $"将安装到 {value}";
            }
        }
    }

    public string InstallLocationHint => $"安装位置：{InstallPath}";

    public double InstallProgress
    {
        get => _installProgress;
        set
        {
            var normalizedValue = Math.Clamp(value, 0, 100);
            if (Math.Abs(normalizedValue - _installProgress) < 0.001)
            {
                return;
            }

            _installProgress = normalizedValue;
            OnPropertyChanged();
        }
    }
    private double _installProgress;

    public string InstallProgressText
    {
        get => _installProgressText;
        set
        {
            if (value == _installProgressText) return;
            _installProgressText = value;
            OnPropertyChanged();
        }
    }
    private string _installProgressText = string.Empty;

    public string InstallStepText
    {
        get => _installStepText;
        set
        {
            if (value == _installStepText) return;
            _installStepText = value;
            OnPropertyChanged();
        }
    }
    private string _installStepText = string.Empty;

    public string InstallDetailText
    {
        get => _installDetailText;
        set
        {
            if (value == _installDetailText) return;
            _installDetailText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InstallDescription));
        }
    }
    private string _installDetailText = string.Empty;

    public string CurrentFileName
    {
        get => _currentFileName;
        set
        {
            if (value == _currentFileName) return;
            _currentFileName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentFileDisplayName));
        }
    }
    private string _currentFileName = string.Empty;

    public string CurrentFileDisplayName => string.IsNullOrWhiteSpace(CurrentFileName) ? "等待安装任务开始" : CurrentFileName;

    public InstallerProgram InstallerProgram { get; }

    public StandardInstallContext StandardInstallContext => InstallerProgram.StandardInstallContext;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void InstallerProgram_ProgressChanged(object? sender, InstallerProgressChangedEventArgs e)
    {
        InstallProgress = e.ProgressPercentage;
        InstallProgressText = $"{e.ProgressPercentage:0}%";
        InstallStepText = e.StageText;
        InstallDetailText = e.DetailText;
        CurrentFileName = e.CurrentFileName ?? string.Empty;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        else
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }, DispatcherPriority.Send);
        }
    }
}
