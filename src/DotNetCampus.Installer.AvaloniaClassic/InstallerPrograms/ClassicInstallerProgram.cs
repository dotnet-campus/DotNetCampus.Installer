using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

internal class ClassicInstallerProgram : StandardInstallerProgram
{
    public ClassicInstallerProgram(StandardInstallContext context)
    {
        StandardInstallContext = context;
    }

    public override StandardInstallContext StandardInstallContext { get; }


}