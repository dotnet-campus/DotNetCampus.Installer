using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;

using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;
using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaClassic;

internal static class Program
{
    [STAThread]
    // 这里强行异步转同步，而不是 async 的原因是为了避免弄坏 STAThread 特性
    // https://github.com/dotnet/runtime/issues/73099
    public static void Main(string[] args)
    {
       


        [MethodImpl(MethodImplOptions.NoInlining)]
        static int RunAvalonia(string[] args, ClassicInstallerProgram? installerProgram = null)
        {
           return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    private static async Task InitAsync()
    {
        var reader = new PEOverlayContentReader();
        var processPath = Environment.ProcessPath;
        Debug.Assert(processPath != null);
        OverlayInstallerContentInfo? readOverlayInstallerContent = await reader.ReadOverlayInstallerContent(new FileInfo(processPath), new InstallerLogger());

        if (readOverlayInstallerContent is null)
        {
            // 没有找到 OverlayInstallerContentInfo，说明不是 Overlay 安装器，但可以是在调试下，为了提升调试体验，是允许为空的情况，此时可以读取本地的某个文件夹路径
        }

        var overlayInstallerContentStream = readOverlayInstallerContent.Value.ContentStream;

        ReadOnlyDirectoryArchive readOnlyDirectoryArchive = await DirectoryArchive.OpenReadAsync(overlayInstallerContentStream);

    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        appBuilder.With(new FontManagerOptions()
        {
            DefaultFamilyName = "Microsoft YaHei UI",
            FontFallbacks =
            [
                new FontFallback { FontFamily = "Microsoft YaHei" },
            ],
        });
        return appBuilder;
    }
}
