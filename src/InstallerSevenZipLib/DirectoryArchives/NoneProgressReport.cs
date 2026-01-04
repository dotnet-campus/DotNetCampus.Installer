using Microsoft.DotNet.Archive;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

internal class NoneProgressReport : IProgress<ProgressReport>
{
    public void Report(ProgressReport value)
    {
    }
}
