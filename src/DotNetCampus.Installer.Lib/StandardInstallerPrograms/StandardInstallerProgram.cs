using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 标准安装器流程
/// </summary>
public abstract class StandardInstallerProgram
{
    public abstract StandardInstallContext StandardInstallContext { get; }
}