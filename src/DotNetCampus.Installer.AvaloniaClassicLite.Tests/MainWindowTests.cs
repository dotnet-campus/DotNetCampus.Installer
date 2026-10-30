using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DotNetCampus.Installer.AvaloniaClassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace DotNetCampus.Installer.AvaloniaClassicLite.Tests;

/// <summary>使用真实 Skia 后端启动独立的无头测试应用。</summary>
public class TestApplication
{
    /// <summary>禁止 Stub 绘图，确保截图包含真实渲染结果。</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .With(new FontManagerOptions { DefaultFamilyName = "Segoe UI" });
}

[TestClass]
[DoNotParallelize]
public class MainWindowTests
{
    private HeadlessUnitTestSession _session = null!;
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Initialize() => _session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
    [TestCleanup]
    public void Cleanup() => _session.Dispose();

    [TestMethod]
    [DataRow(InstallerPage.License, "Install1")]
    [DataRow(InstallerPage.Options, "InstallOptions")]
    [DataRow(InstallerPage.Progress, "InstallProgress")]
    [DataRow(InstallerPage.Completed, "InstallFinish")]
    public async Task WhenPageIsRenderedThenScreenshotHasDesignDimensions(InstallerPage page, string name)
    {
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                window.ShowPage(page);
                window.Show();
                if (page == InstallerPage.Options)
                {
                    var location = window.FindControl<TextBox>("Location")!;
                    location.Text = @"C:\Users\lyvi\AppData\Local\PowerToys";
                    location.Focus();
                    location.SelectAll();
                }
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var bitmap = window.CaptureRenderedFrame() ?? throw new AssertFailedException("Skia frame was not rendered.");
                var directory = Path.Combine(AppContext.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, name + ".png");
                bitmap.Save(path);
                TestContext.AddResultFile(path);
                Assert.AreEqual(new PixelSize(732, 492), bitmap.PixelSize);
                using var reference = SkiaSharp.SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "Designs", name + ".png"));
                using var actual = SkiaSharp.SKBitmap.Decode(path);
                // Windows ClearType 与 Skia 的抗锯齿不同；逐区域计算，避免大片留白掩盖内容丢失。
                foreach (var region in new[] { new PixelRect(0, 0, 732, 160), new PixelRect(19, 160, 694, 225), new PixelRect(320, 400, 393, 73) })
                {
                    double error = 0;
                    for (var y = region.Y; y < region.Bottom; y++)
                    for (var x = region.X; x < region.Right; x++)
                    {
                        var expected = reference.GetPixel(x, y);
                        var observed = actual.GetPixel(x, y);
                        error += Math.Abs(expected.Red - observed.Red) + Math.Abs(expected.Green - observed.Green) + Math.Abs(expected.Blue - observed.Blue);
                    }
                    var meanError = error / (region.Width * region.Height * 3 * 255d);
                    TestContext.WriteLine($"{name} {region}: normalized mean error = {meanError:F4}");
                    Assert.IsLessThan(0.09, meanError, $"Visual difference in {name}, region {region}.");
                }
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WhenLicenseAcceptanceChangesThenInstallAvailabilityMatches(bool accepted)
    {
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                window.FindControl<CheckBox>("Agreement")!.IsChecked = accepted;
                Assert.AreEqual(accepted, window.FindControl<Button>("InstallButton")!.IsEnabled);
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task WhenOptionsAreCancelledThenLicensePageIsRestored()
    {
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                window.FindControl<Button>("OptionsButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                window.FindControl<Button>("CancelButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.IsTrue(window.FindControl<Canvas>("LicensePage")!.IsVisible);
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [TestMethod]
    [DataRow("ProductText", 120d, 57d)]
    [DataRow("CancelButton", 599d, 437d)]
    [DataRow("LicensePage", 17d, 158d)]
    public async Task WhenWindowIsLaidOutThenDesignAnchorsMatch(string name, double x, double y)
    {
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var control = window.FindControl<Control>(name)!;
                Assert.AreEqual(new Point(x, y), control.Bounds.Position);
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }
}
