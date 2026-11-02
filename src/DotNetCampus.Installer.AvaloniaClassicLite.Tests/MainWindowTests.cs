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
