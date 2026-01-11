using DotNetCampus.Cli.Compiler;

namespace DotNetCampus.Installer.Lib.Commandlines;

/// <summary>
/// 安装参数，这是用于 Boost 情况，将参数传递给安装包 UI 程序。这个类型用在安装包 UI 程序解析命令行参数，获取安装参数
/// </summary>
[Command(VerbName)]
public class InstallOptions
{
    public const string VerbName = "install";

    /// <summary>
    /// 安装包入口 Boost 进程的进程 ID，用于安装包 UI 程序向 Boost 进程发送消息，通知其安装进度等信息
    /// </summary>
    [Option(BoostPidOptionName)]
    public required string BoostPid { get; init; }

    public const string BoostPidOptionName = "BoostPid";

    /// <summary>
    /// 安装包的环境界面的窗口句柄，用于安装包 UI 程序主窗口准备完成时，关闭欢迎界面
    /// </summary>
    /// 为什么可能需要欢迎界面呢？这是因为采用 Boost 方式时，需要先解压缩出安装包 UI 程序，然后再启动安装包 UI 程序。整个过程需要一些时间，且还会在过程遇到杀毒软件扫描的问题。这个时候为了提升用户体验就需要一个欢迎界面来遮挡这个过程
    [Option(SplashScreenWindowHandlerOptionName)]
    public long? SplashScreenWindowHandler { get; init; }

    public const string SplashScreenWindowHandlerOptionName = "SplashScreenWindowHandler";
}

[Command("debug show-content")]
public class DebugShowInstallerContentOption
{
}