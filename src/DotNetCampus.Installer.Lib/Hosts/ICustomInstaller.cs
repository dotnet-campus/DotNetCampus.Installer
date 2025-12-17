namespace DotNetCampus.Installer.Lib.Hosts;

/// <summary>
/// 使用进程内自定义安装器的接口标记
/// </summary>
/// <remarks>
/// 默认情况下，都是将独立的安装器打包作为资源，启动的时候解压缩出来运行。默认的情况的方法会导致安装包启动速度比较慢，毕竟需要解压缩和被杀毒扫描。自定义安装器可以直接立刻运行，但要求 UI 界面安装程序能够支持 AOT 构建
/// </remarks>
public interface ICustomInstaller
{

}