namespace DotNetCampus.Installer.AvaloniaClassic;

/// <summary>安装器启动阶段使用的本地化错误文本。</summary>
public static class InstallerMessages
{
    /// <summary>获取安装包损坏提示。</summary>
    public static string CorruptedPackageMessage => LocalizedText.Current.CorruptedPackageMessage;

    /// <summary>获取安装包损坏提示标题。</summary>
    public static string CorruptedPackageTitle => LocalizedText.Current.CorruptedPackageTitle;
}
