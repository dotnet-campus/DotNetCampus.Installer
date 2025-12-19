using Avalonia;

using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.Lib.Hosts.Contexts;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Path = System.IO.Path;

namespace DotNetCampus.Installer.AvaloniaSample;

// 可作为 AOT Lib 被使用，这样可以进行二次压缩，将 14MB 的空安装器，压缩到 6MB 左右
// 但一旦作为 AOT Lib 被使用，就需要有一定的通讯才能实现 Content Resource 的传递。但额外好处是不需要管 libSkiaSharp.dll 和 libHarfBuzzSharp.dll 的加载问题

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var installerProgram = new InstallerProgram();
        var checkEnvironmentSuccess = installerProgram.CheckEnvironment();
        if (!checkEnvironmentSuccess)
        {
            // 环境不满足安装要求，直接退出
            return -1;
        }

        // 先解压缩资产文件，确保在 Avalonia 初始化前完成
        // 解压 libHarfBuzzSharp.dll 和 libSkiaSharp.dll 文件。不需要加载 av_libglesv2.dll 库，原因是开了软渲染

        var context = installerProgram.StandardInstallContext;
        var assemblyManifestResourceInfo = new AssemblyManifestResourceInfo(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.AvaloniaSample.Assets.SkiaX86.assets");
        using (var stream = assemblyManifestResourceInfo.GetManifestResourceStream())
        {
            var libFolder = Path.Join(context.WorkingFolder.FullName, "Lib");

            DirectoryArchive.Decompress(stream, Directory.CreateDirectory(libFolder));

            var libSkiaSharpFile = Path.Join(libFolder, "libSkiaSharp.dll");
            var libHarfBuzzSharpFile = Path.Join(libFolder, "libHarfBuzzSharp.dll");

            NativeLibrary.Load(libSkiaSharpFile);
            NativeLibrary.Load(libHarfBuzzSharpFile);
        }

        var returnResult = RunAvalonia(args, installerProgram);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int RunAvalonia(string[] args, InstallerProgram? installerProgram = null)
        {
            return BuildAvaloniaAppInner(installerProgram)
                 .StartWithClassicDesktopLifetime(args);
        }

        // 尝试删除垃圾文件
        return returnResult;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => BuildAvaloniaAppInner();

    private static AppBuilder BuildAvaloniaAppInner(InstallerProgram? installerProgram = null)
    {
        return AppBuilder.Configure<App>(() => new App(installerProgram))
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