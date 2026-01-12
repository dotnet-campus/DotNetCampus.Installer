using DotNetCampus.Cli.Compiler;

namespace DotNetCampus.Installer.Lib.Commandlines;

/// <summary>
/// 显示安装包内容的调试命令
/// </summary>
[Command("debug list-content")]
public class DebugListInstallerContentOption
{
    [Option('o', "Output")]
    public string? OutputFile { get; set; }
}