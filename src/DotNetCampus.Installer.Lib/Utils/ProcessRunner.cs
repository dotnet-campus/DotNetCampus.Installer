using Microsoft.Win32.SafeHandles;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using DotNetCampus.Installer.Lib.Logging;
using static Windows.Win32.PInvoke;

namespace DotNetCampus.Installer.Lib.Utils;

public static class ProcessRunner
{
    /// <summary>
    /// 降权启动
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="arguments"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// 实现细节请参阅 https://blog.walterlv.com/post/start-process-with-lowered-uac-privileges.html
    [SupportedOSPlatform("windows6.0.6000")]
    public static unsafe bool StartProcessWithShellProcessToken(string fileName, string? arguments, InstallerLogger logger)
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
        {
            Process.Start(new ProcessStartInfo(fileName, arguments ?? string.Empty)
            {
                WorkingDirectory = Path.GetDirectoryName(fileName)
            });
            return true;
        }

        var shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            logger.WriteLog("[ProcessRunner] StartProcessWithShellProcessToken GetShellWindow Failed");
            return false;
        }

        GetWindowThreadProcessId(shellWindow, out var processId);

        if (processId == 0)
        {
            logger.WriteLog("[ProcessRunner] StartProcessWithShellProcessToken GetWindowThreadProcessId Failed");
            return false;
        }

        SafeHandle? processHandle = null;
        SafeFileHandle? shellToken = null;
        SafeFileHandle? duplicateToken = null;
        try
        {
            processHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, processId);
            if (processHandle.IsInvalid)
            {
                var errorCode = Marshal.GetLastPInvokeError();
                logger.WriteLog($"[ProcessRunner] StartProcessWithShellProcessToken OpenProcess Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
                return false;
            }

            if (!OpenProcessToken(processHandle, (TOKEN_ACCESS_MASK) 0x02000000, out shellToken))
            {
                var errorCode = Marshal.GetLastPInvokeError();
                logger.WriteLog($"[ProcessRunner] StartProcessWithShellProcessToken OpenProcessToken Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
                return false;
            }

            if (!DuplicateTokenEx(shellToken, (TOKEN_ACCESS_MASK) 0x02000000, null,
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out duplicateToken))
            {
                var errorCode = Marshal.GetLastPInvokeError();
                logger.WriteLog($"[ProcessRunner] StartProcessWithShellProcessToken DuplicateTokenEx Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
                return false;
            }

            var startupInfo = new STARTUPINFOW
            {
                cb = (uint) Unsafe.SizeOf<STARTUPINFOW>(),
            };
            var commandLine = $@"""{fileName}"" {arguments}";
            Span<char> lpCommandLine = stackalloc char[commandLine.Length + 1];
            commandLine.AsSpan().CopyTo(lpCommandLine);

            var result = CreateProcessWithToken(duplicateToken, 0, null, ref lpCommandLine,
                0, null, Path.GetDirectoryName(fileName)!, in startupInfo, out var information);
            if (!result)
            {
                var errorCode = Marshal.GetLastPInvokeError();
                logger.WriteLog($"[ProcessRunner] StartProcessWithShellProcessToken CreateProcessWithTokenW Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
                return false;
            }
            else
            {
                CloseHandle(information.hProcess);
                CloseHandle(information.hThread);
            }

            return true;
        }
        finally
        {
            if (processHandle?.IsInvalid is false)
            {
                CloseHandle((HANDLE) processHandle.DangerousGetHandle());
            }

            if (shellToken?.IsInvalid is false)
            {
                CloseHandle((HANDLE) shellToken.DangerousGetHandle());
            }

            if (duplicateToken?.IsInvalid is false)
            {
                CloseHandle((HANDLE) duplicateToken.DangerousGetHandle());
            }
        }
    }
}
