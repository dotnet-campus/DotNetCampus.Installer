using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.Configurations;

namespace DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

public class ClassicInstallerProgram : StandardInstallerProgram
{
    public ClassicInstallerProgram(StandardInstallContext context, IAppConfigurator appConfigurator)
    {
        StandardInstallContext = context;
        AppConfigurator = appConfigurator;

        DeleteFolderDelayUntilReboot(context.WorkingFolder);
    }

    public IAppConfigurator AppConfigurator { get; }
    public override StandardInstallContext StandardInstallContext { get; }
}