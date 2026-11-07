using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DotNetCampus.Installer.Lib.EnvironmentCheckers;

/// <summary>
/// 用于检测环境
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public static class EnvironmentChecker
{
    /// <summary>
    /// 检测环境
    /// </summary>
    /// <returns></returns>
    public static EnvironmentCheckResult CheckEnvironment()
    {
        EnvironmentCheckResultType type = EnvironmentCheckResultType.FailedWithUnknownError;
        using var hModule = PInvoke.LoadLibrary("Kernel32.dll");
        if (hModule.IsInvalid)
        {
            // 应该不可能吧，居然加载 Kernel32.dll 失败了
            return new EnvironmentCheckResult(type);
        }

        IntPtr hFarProc = PInvoke.GetProcAddress(hModule, "AddDllDirectory");
        if (hFarProc != IntPtr.Zero)
        {
            // 正常的 dotnet core 依赖环境正常
            //Console.WriteLine("Success. Either running on Win8+, or KB2533623 is installed");
            type = EnvironmentCheckResultType.Passed;
        }
        else
        {
            //Console.WriteLine("Likely running on Win7 or older OS, and KB2533623 is not installed");

            // [操作系统版本 - Win32 apps Microsoft Learn](https://learn.microsoft.com/zh-cn/windows/win32/sysinfo/operating-system-version )
            if (Environment.OSVersion.Version < new Version(6, 1))
            {
                //PInvoke.MessageBox(HWND.Null, $"不支持 Win7 以下系统，当前系统版本 {Environment.OSVersion}", "系统版本过低", MESSAGEBOX_STYLE.MB_OK);
                // 系统版本过低，无法继续安装，不需要挣扎
                type = EnvironmentCheckResultType.FailedWithObsoleteOs;
            }
            else
            {
                // 后续可以决定在这里帮助安装补丁
                type = EnvironmentCheckResultType.FailedWithMissingPatch;
            }
        }

        return new EnvironmentCheckResult(type);
    }

    /// <summary>
    /// 检测环境和弹出提示
    /// </summary>
    /// <returns></returns>
    public static bool CheckEnvironmentAndShowMessageBox()
    {
        var environmentCheckResult = EnvironmentChecker.CheckEnvironment();
        switch (environmentCheckResult.ResultType)
        {
            case EnvironmentCheckResultType.Passed:
                // 正常的 dotnet core 依赖环境正常
                return true;
            case EnvironmentCheckResultType.FailedWithObsoleteOs:
                PInvoke.MessageBox(HWND.Null, $"不支持 Win7 以下系统，当前系统版本 {Environment.OSVersion}", "系统版本过低",
                    MESSAGEBOX_STYLE.MB_OK);
                return false;
            case EnvironmentCheckResultType.FailedWithMissingPatch:
                // 后续可以考虑在这里帮助安装补丁
                // 安装完成之后需要重启，重启最好写入到注册表的 RunOnce 里面，这样大部分杀毒软件都不会拦截安装包在重启之后重新运行
                // 注册表的 RunOnce 路径是 HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce
                PInvoke.MessageBox(HWND.Null, $"系统环境异常，缺少 KB2533623 补丁", "系统环境异常", MESSAGEBOX_STYLE.MB_OK);
                return false;
            case EnvironmentCheckResultType.FailedWithUnknownError:
                PInvoke.MessageBox(HWND.Null, $"环境检测出现未知错误", "安装失败", MESSAGEBOX_STYLE.MB_OK);
                return false;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}