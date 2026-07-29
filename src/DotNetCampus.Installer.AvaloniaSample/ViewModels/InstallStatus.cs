namespace DotNetCampus.Installer.AvaloniaSample.ViewModels;

public enum InstallStatus
{
    /// <summary>
    /// 准备安装
    /// </summary>
    Ready,

    /// <summary>
    /// 安装中
    /// </summary>
    Installing,

    /// <summary>
    /// 安装完成
    /// </summary>
    Finished,

    /// <summary>
    /// 安装出错
    /// </summary>
    Error,
}