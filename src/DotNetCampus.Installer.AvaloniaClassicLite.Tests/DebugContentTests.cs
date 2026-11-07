using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetCampus.Installer.AvaloniaClassicLite.Tests;

[TestClass]
public class DebugContentTests
{
    [TestMethod]
    [Timeout(15_000)]
    public async Task WhenDebugConfigurationIsLoadedThenInstallContextHasSampleProduct()
    {
        var folder = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "DebugContent"));
        var text = await File.ReadAllTextAsync(Path.Combine(folder.FullName, "Installer.coin"));
        var configurator = new MemoryConfigurationRepo(CoinConfigurationSerializer.Deserialize(text)).CreateAppConfigurator();
        using var archive = new FakeDirectoryArchive(folder);
        var context = configurator.Of<StandardInstallerConfiguration>().CreateInstallContext(archive, folder);
        Assert.AreEqual("DotNetCampus Installer (Debug)", context.DisplayProductName);
    }

    [TestMethod]
    [Timeout(15_000)]
    [DataRow("libSkiaSharp.dll")]
    [DataRow("libHarfBuzzSharp.dll")]
    [DataRow("Sample/readme.txt")]
    public void WhenDebugProjectIsBuiltThenRequiredContentIsCopied(string relativePath)
    {
        Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, "DebugContent", relativePath)));
    }
}
