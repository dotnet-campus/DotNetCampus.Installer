using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

/// <summary>
/// 安装程序
/// </summary>
public class InstallerProgram : StandardInstallerProgram
{
    public InstallerProgram()
    {
        StandardInstallContext = InstallContextBuilder.Build();
    }

    public override StandardInstallContext StandardInstallContext { get; }
}