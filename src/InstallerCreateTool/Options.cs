using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace InstallerCreateTool;

[Command("build boost")]
internal class Options : ICommandHandler
{
    /// <summary>
    /// 是否强行使用 UTF-8 编码作为控制台输出
    /// </summary>
    [Option()]
    public bool? ForceUtf8ConsoleOutput { get; init; }

    /// <summary>
    /// 被打包进去的文件夹
    /// </summary>
    [Option()]
    public required string PackingFolder { get; init; }

    /// <summary>
    /// 安装包最终输出的文件夹
    /// </summary>
    [Option()]
    public string? InstallerOutputFolder { get; init; }

    /// <summary>
    /// 安装启动器项目的文件夹路径
    /// </summary>
    [Option()]
    public required string InstallerBoostProjectFolderPath { get; init; }

    /// <summary>
    /// 项目名，如 Installer.Boost.csproj
    /// </summary>
    [Option()]
    public required string InstallerBoostProjectName { get; init; }

    /// <summary>
    /// 图标文件的路径，安装包的图标文件。将被拷贝到 InstallerBoostProjectFolderPath\Assets\Icon.ico 文件路径
    /// </summary>
    [Option()]
    public string? InstallerIconFilePath { get; init; }

    /// <summary>
    /// 欢迎界面图片的路径，安装包的欢迎界面。将被拷贝到 InstallerBoostProjectFolderPath\Assets\SplashScreen.png 文件路径
    /// </summary>
    [Option()]
    public string? SplashScreenFilePath { get; init; }

    public async Task<int> RunAsync()
    {
        var option = this;

        if (option.ForceUtf8ConsoleOutput is true)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        var installerOutputFolder = option.InstallerOutputFolder;

        var installerBoostProjectFolder = option.InstallerBoostProjectFolderPath;
        var installerBoostProjectName = option.InstallerBoostProjectName;

        var installerBoostProjectPath = Path.Join(installerBoostProjectFolder, installerBoostProjectName);

        var installerIconFilePath = option.InstallerIconFilePath;
        if (File.Exists(installerIconFilePath))
        {
            File.Copy(installerIconFilePath, Path.Join(installerBoostProjectFolder, "Assets", "icon.ico"));
        }

        var splashScreenFilePath = option.SplashScreenFilePath;
        if (File.Exists(splashScreenFilePath))
        {
            File.Copy(splashScreenFilePath, Path.Join(installerBoostProjectFolder, "Assets", "SplashScreen.png"));
        }

        Console.WriteLine($"开始制作安装包资产文件");

        var resourceAssetsName = "Resource.assets";
        var resourceAssetsFile = Path.Join(installerBoostProjectFolder, "Assets", resourceAssetsName);

        await DirectoryArchive.CompressAsync(new DirectoryInfo(option.PackingFolder), new FileInfo(resourceAssetsFile), Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}")));

        Console.WriteLine($"完成制作安装包资产文件");

        Console.WriteLine($"开始发布安装包 Boost 项目 {installerBoostProjectPath}");

        List<string> argumentList =
        [
            "publish",
            "-r", "win-x86",
            "-tl:off",
        ];
        if (!string.IsNullOrEmpty(installerOutputFolder))
        {
            argumentList.Add("-o");
            argumentList.Add(installerOutputFolder);
        }
        argumentList.Add(installerBoostProjectPath);

        var process = Process.Start("dotnet", argumentList);
        process.WaitForExit();

        Console.WriteLine("打包完成");

        return 0;
    }
}
