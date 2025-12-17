using Avalonia;

using System;

namespace DotNetCampus.Installer.AvaloniaSample;
internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
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
