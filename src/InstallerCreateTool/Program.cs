// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using DotNetCampus.Cli;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using InstallerCreateTool;

#if DEBUG
if (args.Length == 0)
{
    var slnFolder = GetSlnFolder();

    var installerFolder = Path.Join(slnFolder.FullName, @"artifacts\bin\DotNetCampus.Installer.AvaloniaSample\debug\");

    if (!Directory.Exists(installerFolder))
    {
        Console.WriteLine($"打包目录不存在：{installerFolder}");
        return -1;
    }

    var installerFile = Path.Join(installerFolder, "DotNetCampus.Installer.AvaloniaSample.exe");

    var runtimeFolder = Path.Join(installerFolder, @"runtimes\win-x86\native");
    var skiaFile = Path.Join(runtimeFolder, @"libSkiaSharp.dll");
    var harfBuzzFile = Path.Join(runtimeFolder, "libHarfBuzzSharp.dll");

    Debug.Assert(File.Exists(installerFile));
    Debug.Assert(File.Exists(skiaFile));
    Debug.Assert(File.Exists(harfBuzzFile));

    args =
    [
        "build", "overlay",
        "--Installer", installerFile,
        "--File", skiaFile,
        "--File", harfBuzzFile,
    ];

    //var packingFolder =
    //    @"..\..\..\..\DotNetCampus.Installer.Sample\bin\Debug\net9.0-windows\";
    //packingFolder = Path.GetFullPath(packingFolder);

    //if (!Directory.Exists(packingFolder))
    //{
    //    Console.WriteLine($"打包目录不存在：{packingFolder}");
    //    return -1;
    //}

    //var sampleFilePath = Path.Join(packingFolder, "DotNetCampus.Installer.Sample.exe");
    //var installerFilePath = Path.Join(packingFolder, "Installer.exe");
    //if (File.Exists(sampleFilePath))
    //{
    //    File.Move(sampleFilePath, installerFilePath, overwrite: true);
    //}

    //args =
    //[
    //    "boost",
    //    "--PackingFolder", packingFolder,
    //    "--InstallerOutputFolder", "SampleInstallerFolder",
    //    "--InstallerBoostProjectFolderPath", Path.GetFullPath(@"..\..\..\..\DotNetCampus.Installer.Boost\"),
    //    "--InstallerBoostProjectName", "DotNetCampus.Installer.Boost.csproj",
    //    "--InstallerIconFilePath", "不存在的图标.ico",
    //    "--SplashScreenFilePath", "不存在的欢迎界面.png"
    //];
}
#endif

return await CommandLine.Parse(args)
    .AddHandler<Options>()
    .AddHandler<BuildOverlayOptionCommandHandler>()
    .RunAsync();

DirectoryInfo GetSlnFolder()
{
    var currentFolder = AppContext.BaseDirectory;

    while (true)
    {
        var slnFile = Directory.EnumerateFiles(currentFolder, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFile.Any())
        {
            return new DirectoryInfo(currentFolder);
        }

        currentFolder = Path.GetDirectoryName(currentFolder) ?? throw new DirectoryNotFoundException();
    }
}