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

    /// <inheritdoc cref="Lib.StandardInstallerPrograms.StandardInstallContext.InstallRootPath"/>
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
