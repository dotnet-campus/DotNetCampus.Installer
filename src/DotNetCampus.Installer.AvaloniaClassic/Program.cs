using System;
using Avalonia;
using Avalonia.Media;

namespace DotNetCampus.Installer.AvaloniaClassic;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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
