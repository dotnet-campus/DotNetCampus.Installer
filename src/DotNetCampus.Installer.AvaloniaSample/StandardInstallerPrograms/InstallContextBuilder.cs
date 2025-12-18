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