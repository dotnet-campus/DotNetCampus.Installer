namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

/// <summary>
/// 安装过程的上下文信息
/// </summary>
/// <param name="Configuration">配置</param>
/// <param name="SplashScreenWindowHandler">欢迎界面的句柄</param>
public record InstallContext(InstallerHostConfiguration Configuration, IntPtr SplashScreenWindowHandler)
{

}