using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using DotNetCampus.Installer.AvaloniaClassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace DotNetCampus.Installer.AvaloniaClassicLite.Tests;

/// <summary>使用真实 Skia 后端启动独立的无头测试应用。</summary>
public class TestApplication
{
    /// <summary>配置无头测试应用的渲染后端和默认字体。</summary>
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
    [Timeout(15_000)]
    public void Initialize() => _session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
    [TestCleanup]
    [Timeout(15_000)]
    public void Cleanup() => _session?.Dispose();

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
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
                window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var screenshot = window.CaptureRenderedFrame();
                Assert.IsNotNull(screenshot, "The headless renderer did not produce a frame.");
                var folder = Path.Combine(TestContext.TestRunResultsDirectory!, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, name + ".png");
                screenshot.Save(path);
                TestContext.AddResultFile(path);
                Assert.AreEqual(new PixelSize(732, 492), screenshot.PixelSize);
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
    [DataRow("SmallIcon")]
    [DataRow("ProductIcon")]
    public async Task WhenIconIsNotConfiguredThenClassicInstallerArtworkIsUsed(string controlName)
    {
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                var content = window.FindControl<ContentControl>(controlName)!.Content;
                Assert.IsInstanceOfType<Viewbox>(content);
                var canvas = (Canvas)((Viewbox)content).Child!;
                Assert.AreEqual(6, canvas.Children.OfType<Avalonia.Controls.Shapes.Polygon>().Count());
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WhenLicenseAcceptanceChangesThenInstallAvailabilityMatches(bool accepted)
    {
        // Headless 11.3.9 会内联完成延续；必须异步传播成功和异常，避免 MSTest 清理在 UI 线程等待自身。
        // ContinueWith 不使用 ExecuteSynchronously；此处 GetResult 只读取已完成任务，不阻塞等待。
        await _session.Dispatch(() =>
        {
            var window = new MainWindow();
            try
            {
                window.FindControl<CheckBox>("Agreement")!.IsChecked = accepted;
                Assert.AreEqual(accepted, window.FindControl<Button>("InstallButton")!.IsEnabled);
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    [DataRow(null, "OptionsPage")]
    [DataRow("", "OptionsPage")]
    [DataRow(" \r\n\t", "OptionsPage")]
    [DataRow("License terms", "LicensePage")]
    public async Task WhenLicenseIsConfiguredThenInitialPageMatches(string? license, string pageName)
    {
        await _session.Dispatch(() =>
        {
            var window = CreateWindow(license);
            try { Assert.IsTrue(window.FindControl<Canvas>(pageName)!.IsVisible); }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    [DataRow("InstallButton", true)]
    [DataRow("OkButton", false)]
    [DataRow("OptionsButton", false)]
    public async Task WhenLicenseIsMissingThenOptionsButtonsMatch(string buttonName, bool visible)
    {
        await _session.Dispatch(() =>
        {
            var window = CreateWindow(null);
            try { Assert.AreEqual(visible, window.FindControl<Button>(buttonName)!.IsVisible); }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task WhenLicenseIsMissingThenInstallIsEnabledWithoutAgreement()
    {
        await _session.Dispatch(() =>
        {
            var window = CreateWindow(null);
            try { Assert.IsTrue(window.FindControl<Button>("InstallButton")!.IsEnabled); }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task WhenOptionsHomeIsCancelledThenWindowCloses()
    {
        await _session.Dispatch(() =>
        {
            var window = CreateWindow(null);
            var closed = false;
            window.Closed += (_, _) => closed = true;
            try
            {
                window.Show();
                window.FindControl<Button>("CancelButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.IsTrue(closed);
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task WhenOptionsHomeHasEmptyLocationThenInstallDoesNotLeaveOptions()
    {
        await _session.Dispatch(() =>
        {
            var window = CreateWindow(null);
            try
            {
                window.FindControl<TextBox>("Location")!.Text = " ";
                window.FindControl<Button>("InstallButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.IsTrue(window.FindControl<Canvas>("OptionsPage")!.IsVisible);
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
    [DataRow("SmallIcon")]
    [DataRow("ProductIcon")]
    public async Task WhenIconIsConfiguredThenProductImageIsUsed(string controlName)
    {
        await _session.Dispatch(() =>
        {
            var folder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            folder.Create();
            TestContext.WriteLine(folder.FullName);
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(32, 32)))
                bitmap.Save(Path.Combine(folder.FullName, "InstallerIcon"));
            var window = CreateWindow(null, folder);
            try
            {
                var image = window.FindControl<ContentControl>(controlName)!.Content as Image;
                Assert.IsNotNull(image?.Source);
            }
            finally { window.Close(); }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }

    private static MainWindow CreateWindow(string? license, DirectoryInfo? iconFolder = null)
    {
        var configurator = new MemoryConfigurationRepo().CreateAppConfigurator();
        configurator.Of<ClassicInstallerConfiguration>().LicenseText = license;
        configurator.Of<ClassicInstallerConfiguration>().IconRelativePath = iconFolder is null ? null : "Temp/Product.png";
        var context = new StandardInstallContext
        {
            ProductCodeGuid = Guid.NewGuid(),
            ProductName = "TestProduct",
            ProductFamily = "TestFamily",
            ContentResourceAssetsInfo = null,
            SplashScreenResourceAssetsInfo = null,
            WorkingFolder = iconFolder ?? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")))
        };
        return new MainWindow(new ClassicInstallerProgram(context, configurator));
    }

    [TestMethod]
    [Timeout(15_000)]
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
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }
}
