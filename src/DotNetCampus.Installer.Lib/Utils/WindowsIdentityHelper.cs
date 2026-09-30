using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.Utils;

internal static class WindowsIdentityHelper
{
    /// <summary>
    /// 当前进程是否管理员权限
    /// </summary>
    /// <returns></returns>
    public static bool IsAdministratorRole()
    {
        return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
