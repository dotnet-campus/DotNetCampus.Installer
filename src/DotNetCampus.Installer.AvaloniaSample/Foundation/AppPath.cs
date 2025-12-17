using System.IO;

namespace DotNetCampus.Installer.AvaloniaSample.Foundation;

public class AppPath
{
    public AppPath()
    {
        var tempPath = Path.GetTempPath();
        var workingFolder = Path.Join(tempPath, $"Installer_{Path.GetRandomFileName()}");
        WorkingFolder = Directory.CreateDirectory(workingFolder);
    }

    public DirectoryInfo WorkingFolder { get; }
}