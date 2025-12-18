using System;
using System.Linq;
using System.Reflection;
using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

/// <summary>
/// 安装器上下文构建器
/// </summary>
/// 独立文件是为了更方便更改
class InstallContextBuilder
{
    /// <summary>
    /// 构建标准安装程序的安装上下文
    /// </summary>
    /// <returns></returns>
    public static StandardInstallContext Build()
    {
        var assembly = typeof(InstallContextBuilder).Assembly;
        var version = assembly.GetCustomAttributes<AssemblyFileVersionAttribute>().First().Version;

        var standardInstallContext = new StandardInstallContext()
        {
            // 这里不能采用 Guid.NewGuid 哦，因为写一个固定的常量，且确保和其他软件不相同
            ProductCodeGuid = Guid.Parse("{924C2305-0A44-4610-906B-E202892E82DA}"),

            SplashScreenResourceAssetsInfo = null,
            ContentResourceAssetsInfo = new AssemblyManifestResourceInfo(assembly, "DotNetCampus.Installer.AvaloniaSample.Assets.ContentResource.assets"),
            
            ProductName = "InstallerAvaloniaSample",
            ProductFamily = "DotNetCampus",

            // 示例采用程序集版本号作为安装的版本号。实际使用可以采用更多定制
            AppVersion = version,
        };

        return standardInstallContext;
    }
}