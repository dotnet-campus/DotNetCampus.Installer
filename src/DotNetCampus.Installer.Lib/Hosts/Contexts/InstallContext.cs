using DotNetCampus.Installer.Lib.SplashScreens;

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
}