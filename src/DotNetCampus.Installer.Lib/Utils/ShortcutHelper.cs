using System.Runtime.Versioning;

using Windows.Win32.System.Com;

namespace DotNetCampus.Installer.Lib.Utils;

public static class ShortcutHelper
{
    /// <summary>
    /// 创建一个快捷方式
    /// </summary>
    /// <param name="lnkFilePath">快捷方式的完全限定路径。</param>
    /// <param name="workDir"></param>
    /// <param name="args">快捷方式启动程序时需要使用的参数。</param>
    /// <param name="targetPath">快捷方式指向的目标路径。</param>
    [SupportedOSPlatform("windows5.1.2600")]
    public static unsafe void CreateShortcut(string lnkFilePath, string targetPath, string workDir, string args = "")
    {
        var shellLinkW = ShellLinkProvider.CreateShellLink();
        shellLinkW->SetPath(targetPath);
        shellLinkW->SetArguments(args);
        shellLinkW->SetWorkingDirectory(workDir);

        shellLinkW->QueryInterface(out IPersistFile* persistFile);
        //persistFile->SaveCompleted(lnkFilePath);
        persistFile->Save(lnkFilePath, false);
    }
}
