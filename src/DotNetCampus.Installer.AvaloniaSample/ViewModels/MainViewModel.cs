using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaSample.ViewModels;

public class MainViewModel
{
    public MainViewModel(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
    }

    public InstallerProgram InstallerProgram { get; }

    public StandardInstallContext StandardInstallContext => InstallerProgram.StandardInstallContext;
}
