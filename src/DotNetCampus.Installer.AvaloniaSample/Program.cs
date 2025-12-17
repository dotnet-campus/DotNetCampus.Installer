using Avalonia;

using DotNetCampus.Installer.AvaloniaSample.Foundation;
using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Shapes;
using Path = System.IO.Path;

namespace DotNetCampus.Installer.AvaloniaSample;
internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 先解压缩资产文件，确保在 Avalonia 初始化前完成
        // 解压 libHarfBuzzSharp.dll 和 libSkiaSharp.dll 文件。不需要加载 av_libglesv2.dll 库，原因是开了软渲染
        var appInfo = new AppInfo();
        var appPath = appInfo.AppPath;
        
        var assemblyManifestResourceInfo = new AssemblyManifestResourceInfo(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.AvaloniaSample.Assets.SkiaX86.assets");
        using (var stream = assemblyManifestResourceInfo.GetManifestResourceStream())
        {
            var libFolder = Path.Join(appPath.WorkingFolder.FullName, "Lib");

            DirectoryArchive.Decompress(stream, Directory.CreateDirectory(libFolder));


        }

        RunAvalonia(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunAvalonia(string[] args, AppInfo? appInfo = null)
    {
        BuildAvaloniaAppInner(appInfo)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => BuildAvaloniaAppInner();

    private static AppBuilder BuildAvaloniaAppInner(AppInfo? appInfo = null)
    {
        return AppBuilder.Configure<App>(() => new App(appInfo))
            .UsePlatformDetect()
            .WithInterFont()
            .With(new Win32PlatformOptions()
            {
                RenderingMode =
                [
                    // 明确设置使用软渲染，这样可以不加载 5MB 的 av_libglesv2.dll 库
                    Win32RenderingMode.Software
                ]
            })
            .LogToTrace();
    }
}