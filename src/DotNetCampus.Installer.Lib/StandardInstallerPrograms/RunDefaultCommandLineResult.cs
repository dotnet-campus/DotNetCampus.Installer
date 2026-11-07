namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 执行默认命令行的结果
/// </summary>
public readonly struct RunDefaultCommandLineResult()
{
    /// <summary>
    /// 是否应该退出安装进程
    /// </summary>
    public bool ShouldExitsInstallerProcess { get; init; }

    /// <summary>
    /// 退出代码，仅当 <see cref="ShouldExitsInstallerProcess"/> 为 true 时有效
    /// </summary>
    public int ExitCode { get; init; }
}