using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassicSimple.Tests;

public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

[TestClass]
[DoNotParallelize]
public class InstallerTests
{
    private HeadlessUnitTestSession _session = null!;

    [TestInitialize]
    public void Initialize() => _session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

    [TestCleanup]
    public void Cleanup() => _session.Dispose();

    [TestMethod]
    public Task WhenLicenseAcceptedThenInstallEnabled() => _session.Dispatch(() =>
    {
        var model = CreateModel();
        var window = new MainWindow(model);
        window.Show();
        window.GetControl<CheckBox>("LicenseAgreement").IsChecked = true;
        Assert.IsTrue(window.GetControl<Button>("InstallButton").IsEnabled);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public Task WhenLicenseNotAcceptedThenInstallDisabled() => _session.Dispatch(() =>
    {
        var window = new MainWindow(CreateModel());
        window.Show();
        Assert.IsFalse(window.GetControl<Button>("InstallButton").IsEnabled);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public void WhenOptionsCommittedThenPathChanges()
    {
        var model = CreateModel();
        model.OpenOptions();
        model.DraftPath = @"C:\Applications\Example";
        model.CommitOptions();
        Assert.AreEqual(@"C:\Applications\Example", model.InstallationPath);
    }

    [TestMethod]
    public void WhenOptionsCancelledThenPathUnchanged()
    {
        var model = CreateModel();
        model.OpenOptions();
        model.DraftPath = @"C:\Applications\Other";
        model.CancelOptions();
        Assert.AreEqual(@"C:\Users\Example\AppData\Local\Example", model.InstallationPath);
    }

    [TestMethod]
    public void WhenPathRelativeThenOptionsStayOpen()
    {
        var model = CreateModel();
        model.OpenOptions();
        model.DraftPath = "relative";
        model.CommitOptions();
        Assert.AreEqual(InstallerPage.Options, model.Page);
    }

    [TestMethod]
    public void WhenLicenseNotAcceptedThenCannotBegin() => Assert.IsFalse(CreateModel().BeginInstallation());

    [TestMethod]
    public void WhenProgressReportedThenFileDisplayed()
    {
        var model = CreateModel();
        model.ReportProgress(new ClassicInstallerProgressChangedEventArgs(18, ClassicInstallerProgressStage.DeployingApplicationFiles, "Example.dll"));
        Assert.AreEqual("Example.dll", model.Status);
    }

    [TestMethod]
    public void WhenInstallationFailsThenErrorDisplayed()
    {
        var model = CreateModel();
        model.CompleteInstallation("Access denied");
        Assert.AreEqual("Access denied", model.Error);
    }

    [TestMethod]
    [DataRow(InstallerPage.License, "LicensePage")]
    [DataRow(InstallerPage.Options, "OptionsPage")]
    [DataRow(InstallerPage.Progress, "ProgressPage")]
    [DataRow(InstallerPage.Finished, "FinishedPage")]
    public Task WhenPageSelectedThenOnlyCorrespondingPageVisible(InstallerPage page, string control) => _session.Dispatch(() =>
    {
        var model = CreateModel();
        SelectPage(model, page);
        var window = new MainWindow(model);
        window.Show();
        var visible = new[] { "LicensePage", "OptionsPage", "ProgressPage", "FinishedPage" }
            .Where(name => window.GetControl<Grid>(name).IsVisible).ToArray();
        CollectionAssert.AreEqual(new[] { control }, visible);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public Task WhenOptionsButtonClickedThenEditorAppears() => _session.Dispatch(() =>
    {
        var window = new MainWindow(CreateModel());
        window.Show();
        window.GetControl<Button>("OptionsButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsTrue(window.GetControl<Grid>("OptionsPage").IsVisible);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public Task WhenProgressShownThenBarUsesReportedValue() => _session.Dispatch(() =>
    {
        var model = CreateModel();
        model.ReportProgress(new ClassicInstallerProgressChangedEventArgs(18, ClassicInstallerProgressStage.DeployingApplicationFiles, null));
        var window = new MainWindow(model);
        window.Show();
        Assert.AreEqual(18, window.GetControl<ProgressBar>("InstallationProgress").Value);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public Task WhenWindowLaidOutThenFooterMatchesReferenceGeometry() => _session.Dispatch(() =>
    {
        var window = new MainWindow(CreateModel());
        window.Show();
        window.UpdateLayout();
        Assert.AreEqual(new Size(111, 33), window.GetControl<Button>("InstallButton").Bounds.Size);
        window.Close();
    }, CancellationToken.None);

    [TestMethod]
    public Task WhenFourPagesRenderedThenFramesHaveClientDimensions() => _session.Dispatch(() =>
    {
        var sizes = RenderScreenshots(Path.Join(AppContext.BaseDirectory, "Screenshots"));
        CollectionAssert.AreEqual(Enumerable.Repeat(new PixelSize(728, 450), 4).ToArray(), sizes);
    }, CancellationToken.None);

    public static InstallerViewModel CreateModel(bool withIcon = false) => new("Example Product", @"C:\Users\Example\AppData\Local\Example",
        "MIT License\n\nCopyright (c) Example contributors. All rights reserved.\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software.\n\nTHE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.", withIcon ? CreateIcon() : null);

    private static DrawingImage CreateIcon() => new() { Drawing = new GeometryDrawing
    {
        Brush = Brushes.SteelBlue,
        Geometry = Geometry.Parse("M 8,8 L 84,8 84,84 8,84 Z M 20,20 L 20,72 72,72 72,20 Z")
    } };

    private static void SelectPage(InstallerViewModel model, InstallerPage page)
    {
        switch (page)
        {
            case InstallerPage.Options: model.OpenOptions(); break;
            case InstallerPage.Progress:
                model.HasAcceptedLicense = true;
                model.BeginInstallation();
                model.ReportProgress(new ClassicInstallerProgressChangedEventArgs(18, ClassicInstallerProgressStage.DeployingApplicationFiles, "Example Product"));
                break;
            case InstallerPage.Finished: model.CompleteInstallation(); break;
        }
    }

    public static PixelSize[] RenderScreenshots(string folder)
    {
        Directory.CreateDirectory(folder);
        var sizes = new List<PixelSize>();
        foreach (var page in Enum.GetValues<InstallerPage>())
        {
            var model = CreateModel(withIcon: true);
            SelectPage(model, page);
            var window = new MainWindow(model);
            window.Show();
            if (page == InstallerPage.Options)
            {
                var editor = window.GetControl<TextBox>("PathEditor");
                editor.Focus();
                editor.SelectAll();
            }
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
            frame.Save(Path.Join(folder, $"{page}.png"));
            sizes.Add(frame.PixelSize);
            window.Close();
        }
        return sizes.ToArray();
    }
}

internal static class ScreenshotRunner
{
    public static async Task<int> Main(string[] args)
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
        await session.Dispatch(() => InstallerTests.RenderScreenshots(Path.Join(AppContext.BaseDirectory, "Screenshots")), CancellationToken.None);
        return 0;
    }
}
