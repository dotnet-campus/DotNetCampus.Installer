using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

/// <summary>
/// 安装程序
/// </summary>
[SupportedOSPlatform("windows5.0")]
public class InstallerProgram : StandardInstallerProgram
{
    public InstallerProgram()
    {
        StandardInstallContext = InstallContextBuilder.Build();
    }

    public override StandardInstallContext StandardInstallContext { get; }
}