using Avalonia;

using DotNetCampus.Installer.AvaloniaSample.Foundation;
using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Shapes;
using DotNetCampus.Installer.Lib;
using Path = System.IO.Path;

namespace DotNetCampus.Installer.AvaloniaSample;

internal class Program : InstallerHost
{
    public Program(InstallerHostConfiguration configuration) : base(configuration)
    {
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // 先解压缩资产文件，确保在 Avalonia 初始化前完成
        // 解压 libHarfBuzzSharp.dll 和 libSkiaSharp.dll 文件。不需要加载 av_libglesv2.dll 库，原因是开了软渲染

        var builder = InstallerHost.CreateBuilder();
        builder.UseCustomInstallerHost(configuration => new Program(configuration));
        InstallerHost installerHost = builder.Build();
        return installerHost.Run();
    }

    protected override void Install(InstallContext context)
    {
        var appInfo = new AppInfo();
        var appPath = appInfo.AppPath;

        var assemblyManifestResourceInfo = new AssemblyManifestResourceInfo(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.AvaloniaSample.Assets.SkiaX86.assets");
        using (var stream = assemblyManifestResourceInfo.GetManifestResourceStream())
        {
            var libFolder = Path.Join(appPath.WorkingFolder.FullName, "Lib");

            DirectoryArchive.Decompress(stream, Directory.CreateDirectory(libFolder));

            var libSkiaSharpFile = Path.Join(libFolder, "libSkiaSharp.dll");
            var libHarfBuzzSharpFile = Path.Join(libFolder, "libHarfBuzzSharp.dll");

            NativeLibrary.Load(libSkiaSharpFile);
            NativeLibrary.Load(libHarfBuzzSharpFile);
        }

        RunAvalonia(appInfo);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RunAvalonia(AppInfo? appInfo = null)
        {
            BuildAvaloniaAppInner(appInfo)
                .StartWithClassicDesktopLifetime([]);
        }

        // 尝试删除垃圾文件
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