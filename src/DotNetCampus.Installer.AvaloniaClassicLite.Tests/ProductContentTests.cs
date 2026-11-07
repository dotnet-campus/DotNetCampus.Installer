using Avalonia.Controls;
using Avalonia.Headless;
using DotNetCampus.Installer.AvaloniaClassic;
using DotNetCampus.Installer.AvaloniaClassic.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetCampus.Installer.AvaloniaClassicLite.Tests;

[TestClass]
public class ProductContentTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(15_000)]
    [DataRow("ProductText", nameof(MainWindowViewModel.ProductName))]
    [DataRow("TitleText", nameof(MainWindowViewModel.WindowTitle))]
    [DataRow("LicenseText", nameof(MainWindowViewModel.LicenseText))]
    [DataRow("ProcessingText", nameof(MainWindowViewModel.ProductName))]
    public async Task WhenPreviewIsCreatedThenContentComesFromClassicViewModel(string controlName, string propertyName)
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
        // 异步传播 Dispatch 的成功和异常，避免 using 的 Dispose 内联到会话线程并等待自身。
        await session.Dispatch(() =>
        {
            using var model = new MainWindowViewModel(null);
            var window = new MainWindow();
            try
            {
                var expected = typeof(MainWindowViewModel).GetProperty(propertyName)!.GetValue(model);
                Assert.AreEqual(expected, window.FindControl<TextBlock>(controlName)!.Text);
            }
            finally
            {
                window.Close();
            }
        }, TestContext.CancellationToken).ContinueWith(task => task.GetAwaiter().GetResult(),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
    }
}
