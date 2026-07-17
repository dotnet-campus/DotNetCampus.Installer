using dotnetCampus.Configurations.Core;

using DotNetCampus.Installer.Lib.Exceptions;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;
using Microsoft.DotNet.Archive;

namespace DotNetCampus.Installer.Lib.Tests;

[TestClass]
public class StandardInstallerConfigurationTests
{
    [TestMethod(DisplayName = "完整配置应映射到标准安装上下文")]
    [Timeout(10_000)]
    public void WhenConfigurationIsCompleteThenAllConfigurationValuesAreMapped()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var workingFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        var configuration = CreateValidConfiguration();

        var context = configuration.CreateInstallContext(directoryArchive, workingFolder);

        var expected = new ConfigurationSnapshot(
            ProductCodeGuid,
            ProductName,
            DisplayProductName,
            ProductFamily,
            DisplayProductFamily,
            AppVersion,
            LauncherExeRelativePath,
            UninstallDisplayIconRelativePath,
            UninstallDisplayName,
            UninstallDisplayVersion,
            UninstallEstimatedSize,
            UninstallDisplayPublisher,
            UninstallerRelativePath);
        var actual = ConfigurationSnapshot.From(context);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod(DisplayName = "创建上下文时应保留传入的工作目录")]
    [Timeout(10_000)]
    public void WhenCreatingContextThenWorkingFolderIsPreserved()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var workingFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        var configuration = CreateValidConfiguration();

        var context = configuration.CreateInstallContext(directoryArchive, workingFolder);

        Assert.AreSame(workingFolder, context.WorkingFolder);
    }

    [TestMethod(DisplayName = "创建上下文时应保留传入的目录归档")]
    [Timeout(10_000)]
    public async Task WhenCreatingContextThenDirectoryArchiveIsPreserved()
    {
        await using var directoryArchive = new FakeDirectoryArchive([]);
        var workingFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        var configuration = CreateValidConfiguration();
        var context = configuration.CreateInstallContext(directoryArchive, workingFolder);

        var actual = await context.GetOverlayDirectoryArchive();

        Assert.AreSame(directoryArchive, actual);
    }

    [TestMethod(DisplayName = "配置创建的上下文应能解压目录归档内容")]
    [Timeout(10_000)]
    public async Task WhenContextIsCreatedFromConfigurationThenArchiveContentCanBeDecompressed()
    {
        var testFolder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        var archiveFolder = Directory.CreateDirectory(Path.Join(testFolder.FullName, "Archive"));
        var packingFolder = Directory.CreateDirectory(Path.Join(archiveFolder.FullName, "Packing"));
        const string expected = "Installer content";
        await File.WriteAllTextAsync(Path.Join(packingFolder.FullName, "Content.txt"), expected);
        await using var directoryArchive = new FakeDirectoryArchive(archiveFolder);
        var configuration = CreateValidConfiguration();
        var context = configuration.CreateInstallContext(
            directoryArchive,
            Directory.CreateDirectory(Path.Join(testFolder.FullName, "Working")));
        context.MainInstallPath = Path.Join(testFolder.FullName, "Output");
        using var installerProgram = new TestStandardInstallerProgram(context);

        await installerProgram.Decompress();

        var actual = await File.ReadAllTextAsync(Path.Join(context.MainInstallPath, "Content.txt"));
        Assert.AreEqual(expected, actual);
    }

    [TestMethod(DisplayName = "解压目录归档时应报告当前文件和聚合字节进度")]
    [Timeout(10_000)]
    public async Task WhenArchiveIsDecompressedThenCurrentFileAndAggregateByteProgressAreReported()
    {
        var testFolder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        IDirectoryArchiveEntryFile firstEntry = new TestDirectoryArchiveEntryFile(
            @"Packing\First.txt",
            [1, 2, 3],
            [1]);
        IDirectoryArchiveEntryFile secondEntry = new TestDirectoryArchiveEntryFile(
            @"Packing\Nested\Second.txt",
            [4, 5, 6, 7, 8],
            [2]);
        await using var directoryArchive = new FakeDirectoryArchive([firstEntry, secondEntry]);
        var context = CreateValidConfiguration().CreateInstallContext(directoryArchive, testFolder);
        context.MainInstallPath = Path.Join(testFolder.FullName, "Output");
        using var installerProgram = new TestStandardInstallerProgram(context);
        var actual = new List<(
            string CurrentFileName,
            long DecompressedBytes,
            long TotalBytes,
            double ProgressPercentage)>();
        var progress = new InlineProgress<StandardInstallerDecompressProgress>(report => actual.Add((
            report.CurrentFileName,
            report.DecompressedBytes,
            report.TotalBytes,
            report.ProgressPercentage)));

        await installerProgram.Decompress(progress, CancellationToken.None);

        List<(
            string CurrentFileName,
            long DecompressedBytes,
            long TotalBytes,
            double ProgressPercentage)> expected =
        [
            ("First.txt", 0, 8, 0),
            ("First.txt", 1, 8, 12.5),
            ("First.txt", 3, 8, 37.5),
            (@"Nested\Second.txt", 3, 8, 37.5),
            (@"Nested\Second.txt", 5, 8, 62.5),
            (@"Nested\Second.txt", 8, 8, 100)
        ];
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod(DisplayName = "解压开始前取消时应立即抛出取消异常")]
    [Timeout(10_000)]
    public async Task WhenCancellationIsRequestedBeforeDecompressionThenOperationCanceledExceptionIsThrown()
    {
        var testFolder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        IDirectoryArchiveEntryFile entry = new TestDirectoryArchiveEntryFile(
            @"Packing\Content.txt",
            [1, 2, 3],
            [1]);
        await using var directoryArchive = new FakeDirectoryArchive([entry]);
        var context = CreateValidConfiguration().CreateInstallContext(directoryArchive, testFolder);
        context.MainInstallPath = Path.Join(testFolder.FullName, "Output");
        using var installerProgram = new TestStandardInstallerProgram(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            installerProgram.Decompress(progress: null, cancellationTokenSource.Token));
    }

    [TestMethod(DisplayName = "解压过程中取消时应抛出取消异常")]
    [Timeout(10_000)]
    public async Task WhenCancellationIsRequestedDuringDecompressionThenOperationCanceledExceptionIsThrown()
    {
        var testFolder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()));
        using var cancellationTokenSource = new CancellationTokenSource();
        IDirectoryArchiveEntryFile entry = new TestDirectoryArchiveEntryFile(
            @"Packing\Content.txt",
            [1, 2, 3],
            [1, 2],
            cancellationTokenSource.Cancel);
        await using var directoryArchive = new FakeDirectoryArchive([entry]);
        var context = CreateValidConfiguration().CreateInstallContext(directoryArchive, testFolder);
        context.MainInstallPath = Path.Join(testFolder.FullName, "Output");
        using var installerProgram = new TestStandardInstallerProgram(context);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            installerProgram.Decompress(progress: null, cancellationTokenSource.Token));
    }

    [TestMethod(DisplayName = "目录归档为空时应抛出参数为空异常")]
    [Timeout(10_000)]
    public void WhenDirectoryArchiveIsNullThenArgumentNullExceptionIsThrown()
    {
        var configuration = CreateValidConfiguration();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            configuration.CreateInstallContext(null!, new DirectoryInfo(Path.GetTempPath())));
    }

    [TestMethod(DisplayName = "工作目录为空时应抛出参数为空异常")]
    [Timeout(10_000)]
    public void WhenWorkingFolderIsNullThenArgumentNullExceptionIsThrown()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var configuration = CreateValidConfiguration();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            configuration.CreateInstallContext(directoryArchive, null!));
    }

    [TestMethod(DisplayName = "产品代码为空时应抛出安装包配置异常")]
    [Timeout(10_000)]
    public void WhenProductCodeGuidIsEmptyThenConfigurationExceptionIsThrown()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var configuration = CreateValidConfiguration();
        configuration.ProductCodeGuid = Guid.Empty;

        Assert.ThrowsExactly<InstallerConfigurationException>(() =>
            configuration.CreateInstallContext(directoryArchive, new DirectoryInfo(Path.GetTempPath())));
    }

    [TestMethod(DisplayName = "产品名称为空白时应抛出安装包配置异常")]
    [Timeout(10_000)]
    public void WhenProductNameIsWhiteSpaceThenConfigurationExceptionIsThrown()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var configuration = CreateValidConfiguration();
        configuration.ProductName = " ";

        Assert.ThrowsExactly<InstallerConfigurationException>(() =>
            configuration.CreateInstallContext(directoryArchive, new DirectoryInfo(Path.GetTempPath())));
    }

    [TestMethod(DisplayName = "产品族名称为空白时应抛出安装包配置异常")]
    [Timeout(10_000)]
    public void WhenProductFamilyIsWhiteSpaceThenConfigurationExceptionIsThrown()
    {
        using var directoryArchive = new FakeDirectoryArchive([]);
        var configuration = CreateValidConfiguration();
        configuration.ProductFamily = "\t";

        Assert.ThrowsExactly<InstallerConfigurationException>(() =>
            configuration.CreateInstallContext(directoryArchive, new DirectoryInfo(Path.GetTempPath())));
    }

    private static StandardInstallerConfiguration CreateValidConfiguration()
    {
        var configurationRepo = new MemoryConfigurationRepo();
        var configuration = configurationRepo.CreateAppConfigurator().Of<StandardInstallerConfiguration>();
        configuration.ProductCodeGuid = ProductCodeGuid;
        configuration.ProductName = ProductName;
        configuration.DisplayProductName = DisplayProductName;
        configuration.ProductFamily = ProductFamily;
        configuration.DisplayProductFamily = DisplayProductFamily;
        configuration.AppVersion = AppVersion;
        configuration.LauncherExeRelativePath = LauncherExeRelativePath;
        configuration.UninstallDisplayIconRelativePath = UninstallDisplayIconRelativePath;
        configuration.UninstallDisplayName = UninstallDisplayName;
        configuration.UninstallDisplayVersion = UninstallDisplayVersion;
        configuration.UninstallEstimatedSize = UninstallEstimatedSize;
        configuration.UninstallDisplayPublisher = UninstallDisplayPublisher;
        configuration.UninstallerRelativePath = UninstallerRelativePath;
        return configuration;
    }

    private static readonly Guid ProductCodeGuid = Guid.Parse("662B9E4E-D43D-454C-824A-27296A83320D");
    private const string ProductName = "InstallerTests";
    private const string DisplayProductName = "安装器测试产品";
    private const string ProductFamily = "DotNetCampus";
    private const string DisplayProductFamily = "DotNet Campus";
    private const string AppVersion = "2.3.4.5";
    private const string LauncherExeRelativePath = "Application\\Launcher.exe";
    private const string UninstallDisplayIconRelativePath = "Application\\Launcher.exe";
    private const string UninstallDisplayName = "安装器测试产品卸载程序";
    private const string UninstallDisplayVersion = "2.3.4";
    private const int UninstallEstimatedSize = 1024;
    private const string UninstallDisplayPublisher = "dotnet campus";
    private const string UninstallerRelativePath = "Uninstall.exe";

    private sealed record ConfigurationSnapshot(
        Guid ProductCodeGuid,
        string ProductName,
        string DisplayProductName,
        string ProductFamily,
        string DisplayProductFamily,
        string AppVersion,
        string? LauncherExeRelativePath,
        string? UninstallDisplayIconRelativePath,
        string UninstallDisplayName,
        string UninstallDisplayVersion,
        int? UninstallEstimatedSize,
        string UninstallDisplayPublisher,
        string? UninstallerRelativePath)
    {
        public static ConfigurationSnapshot From(StandardInstallContext context)
        {
            return new ConfigurationSnapshot(
                context.ProductCodeGuid,
                context.ProductName,
                context.DisplayProductName,
                context.ProductFamily,
                context.DisplayProductFamily,
                context.AppVersion,
                context.LauncherExeRelativePath,
                context.UninstallDisplayIconRelativePath,
                context.UninstallDisplayName,
                context.UninstallDisplayVersion,
                context.UninstallEstimatedSize,
                context.UninstallDisplayPublisher,
                context.UninstallerRelativePath);
        }
    }

    private sealed class TestStandardInstallerProgram(StandardInstallContext context) : StandardInstallerProgram
    {
        public override StandardInstallContext StandardInstallContext { get; } = context;
    }

    private sealed class TestDirectoryArchiveEntryFile(
        string relativePath,
        byte[] content,
        IReadOnlyList<long> progressTicks,
        Action? progressReported = null) : IDirectoryArchiveEntryFile
    {
        public DirectoryArchiveEntryRelativePath RelativePath { get; } = relativePath;

        public long CompressedFileLength => content.Length;

        public long OriginFileLength => content.Length;

        public async Task CopyToAsync(Stream destinationStream, IProgress<ProgressReport>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(destinationStream);

            foreach (var ticks in progressTicks)
            {
                progress?.Report(new ProgressReport("Decompressing", ticks, content.Length));
                progressReported?.Invoke();
            }

            await destinationStream.WriteAsync(content).ConfigureAwait(false);
        }

        public async Task SaveToFileAsync(FileInfo outputFile, IProgress<ProgressReport>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(outputFile);

            outputFile.Directory?.Create();
            await using var outputStream = outputFile.Create();
            await CopyToAsync(outputStream, progress).ConfigureAwait(false);
        }
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
