using Avalonia.Threading;

using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaSample.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
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
        }
    }
    private InstallStatus _installStatus = InstallStatus.Ready;

    /// <inheritdoc cref="DotNetCampus.Installer.Lib.StandardInstallerPrograms.StandardInstallContext.InstallRootPath"/>
    public string InstallPath
    {
        get => StandardInstallContext.InstallRootPath;
        set
        {
            if (value == StandardInstallContext.InstallRootPath) return;
            StandardInstallContext.InstallRootPath = value;
            OnPropertyChanged();
        }
    }

    public InstallerProgram InstallerProgram { get; }

    public StandardInstallContext StandardInstallContext => InstallerProgram.StandardInstallContext;
    public event PropertyChangedEventHandler? PropertyChanged;

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
