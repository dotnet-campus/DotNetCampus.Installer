using System.Runtime.Versioning;
using DotNetCampus.Installer.Lib.SplashScreens;
using DotNetCampus.Installer.Lib.Utils;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using Microsoft.DotNet.Archive;

namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

/// <summary>
/// 安装过程的上下文信息
/// </summary>
public class InstallContext
{
    /// <summary>
    /// 欢迎界面的句柄
    /// </summary>
    public required IntPtr SplashScreenWindowHandler { get; init; }

    /// <summary>
    /// 工作路径
    /// </summary>
    public required DirectoryInfo WorkingFolder { get; init; }

    /// <summary>
    /// 安装内容的资源信息
    /// </summary>
    public required AssemblyManifestResourceInfo? ContentResourceAssetsInfo { get; init; }

    /// <summary>
    /// 关闭欢迎界面
    /// </summary>
    public void CloseSplashScreenWindow()
    {
        if (SplashScreenWindowHandler != 0)
        {
            SplashScreen.CloseSplashScreenWindow(SplashScreenWindowHandler);
        }
    }

    /// <summary>
    /// 解压缩安装内容资源
    /// </summary>
    public void DecompressContentResource(DirectoryInfo outputFolder, IProgress<ProgressReport>? progress = null)
    {
        if (ContentResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        using var manifestResourceStream = ContentResourceAssetsInfo.Value.GetManifestResourceStream();
        DirectoryArchive.Decompress(manifestResourceStream, outputFolder, progress);
    }

    /// <summary>
    /// 创建一个快捷方式
    /// </summary>
    /// <param name="lnkFilePath">快捷方式的完全限定路径。</param>
    /// <param name="targetPath">快捷方式指向的目标路径。</param>
    /// <param name="workDir"></param>
    /// <param name="args">快捷方式启动程序时需要使用的参数。</param>
    [SupportedOSPlatform("windows5.1.2600")]
    public void CreateShortcut(string lnkFilePath, string targetPath, string workDir, string args = "")
    {
        ShortcutHelper.CreateShortcut(lnkFilePath, targetPath, workDir, args);
    }
}