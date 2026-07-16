using dotnetCampus.Configurations;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;
using Microsoft.DotNet.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

/// <summary>
/// 提供经典界面的标准安装程序。
/// </summary>
public class ClassicInstallerProgram : StandardInstallerProgram
{
    private const double InstallPreparationProgress = 0;
    private const double ExistingInstallationCheckProgress = 8;
    private const double DecompressStartProgress = 12;
    private const double DecompressEndProgress = 90;
    private const double RegisterProgress = 95;
    private const double ShortcutProgress = 98;

    /// <summary>
    /// 创建经典界面的标准安装程序。
    /// </summary>
    /// <param name="context">标准安装上下文。</param>
    /// <param name="appConfigurator">安装包配置读取器。</param>
    public ClassicInstallerProgram(StandardInstallContext context, IAppConfigurator appConfigurator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(appConfigurator);

        StandardInstallContext = context;
        AppConfigurator = appConfigurator;

        DeleteFolderDelayUntilReboot(context.WorkingFolder);
    }

    /// <summary>
    /// 获取安装包配置读取器。
    /// </summary>
    public IAppConfigurator AppConfigurator { get; }

    /// <inheritdoc />
    public override StandardInstallContext StandardInstallContext { get; }

    internal event EventHandler<ClassicInstallerProgressChangedEventArgs>? ProgressChanged;

    internal bool CanLaunchApplication => !string.IsNullOrWhiteSpace(StandardInstallContext.LauncherExeRelativePath);

    /// <inheritdoc />
    public override Task InstallAsync() => InstallAsync(CancellationToken.None);

    internal async Task InstallAsync(CancellationToken cancellationToken)
    {
        Logger.WriteLog($"Install start. Installer PID={Environment.ProcessId}");

        ReportProgress(InstallPreparationProgress, ClassicInstallerProgressStage.PreparingInstallation);
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(ExistingInstallationCheckProgress, ClassicInstallerProgressStage.CheckingExistingInstallation);
        ClearOldVersion();
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(StandardInstallContext.MainInstallPath);
        var logFile = new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, "InstallerLog.txt"));
        Logger.SetLogFile(logFile);

        ReportProgress(DecompressStartProgress, ClassicInstallerProgressStage.DeployingApplicationFiles);
        await DecompressAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(RegisterProgress, ClassicInstallerProgressStage.RegisteringApplication);
        WriteRegister();
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(ShortcutProgress, ClassicInstallerProgressStage.CreatingShortcuts);
        CreateShortcut();
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(100, ClassicInstallerProgressStage.Completed);
    }

    /// <inheritdoc />
    public override Task Decompress() => DecompressAsync(CancellationToken.None);

    internal bool TryLaunchApplication()
    {
        var launcherExeFullPath = StandardInstallContext.GetLauncherExeFullPath();
        if (string.IsNullOrWhiteSpace(launcherExeFullPath) || !File.Exists(launcherExeFullPath))
        {
            Logger.WriteLog($"Can not launch application. Launcher path='{launcherExeFullPath}'.");
            return false;
        }

        return StartProcessWithShellProcessToken(launcherExeFullPath);
    }

    private async Task DecompressAsync(CancellationToken cancellationToken)
    {
        var overlayDirectoryArchive = await StandardInstallContext.GetOverlayDirectoryArchive().ConfigureAwait(false);
        if (overlayDirectoryArchive is not null)
        {
            const string packingPrefix = @"Packing\";
            var overlayEntryList = overlayDirectoryArchive.EntryFileList
                .Where(file => file.RelativePath.RelativePath.StartsWith(packingPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (overlayEntryList.Count == 0)
            {
                throw new InvalidOperationException();
            }

            await ExtractEntriesAsync(
                overlayEntryList,
                static relativePath => relativePath[packingPrefix.Length..],
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var contentResourceAssetsInfo = StandardInstallContext.ContentResourceAssetsInfo;
        if (contentResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        await using var contentStream = contentResourceAssetsInfo.Value.GetManifestResourceStream();
        await using var embeddedDirectoryArchive = await DirectoryArchive.OpenReadAsync(contentStream).ConfigureAwait(false);
        await ExtractEntriesAsync(
            embeddedDirectoryArchive.EntryFileList,
            static relativePath => relativePath,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ExtractEntriesAsync(
        IReadOnlyList<IDirectoryArchiveEntryFile> entryList,
        Func<string, string> relativePathTransform,
        CancellationToken cancellationToken)
    {
        var totalPayloadBytes = entryList.Sum(static file => file.OriginFileLength);
        long completedPayloadBytes = 0;

        foreach (var entry in entryList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = relativePathTransform(entry.RelativePath.RelativePath);
            var outputFile = new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, relativePath));
            var entryProgress = new InlineProgress<ProgressReport>(report =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentFileBytes = Math.Clamp(report.Ticks, 0, entry.OriginFileLength);
                ReportProgress(
                    CalculateDecompressProgress(completedPayloadBytes + currentFileBytes, totalPayloadBytes),
                    ClassicInstallerProgressStage.DeployingApplicationFiles,
                    relativePath);
            });

            ReportProgress(
                CalculateDecompressProgress(completedPayloadBytes, totalPayloadBytes),
                ClassicInstallerProgressStage.DeployingApplicationFiles,
                relativePath);
            await entry.SaveToFileAsync(outputFile, entryProgress).ConfigureAwait(false);

            completedPayloadBytes += entry.OriginFileLength;
            ReportProgress(
                CalculateDecompressProgress(completedPayloadBytes, totalPayloadBytes),
                ClassicInstallerProgressStage.DeployingApplicationFiles,
                relativePath);
        }
    }

    private void ReportProgress(
        double progressPercentage,
        ClassicInstallerProgressStage stage,
        string? currentFileName = null)
    {
        ProgressChanged?.Invoke(
            this,
            new ClassicInstallerProgressChangedEventArgs(progressPercentage, stage, currentFileName));
    }

    private static double CalculateDecompressProgress(long completedPayloadBytes, long totalPayloadBytes)
    {
        if (totalPayloadBytes <= 0)
        {
            return DecompressEndProgress;
        }

        var ratio = Math.Clamp(completedPayloadBytes / (double) totalPayloadBytes, 0, 1);
        return DecompressStartProgress + (DecompressEndProgress - DecompressStartProgress) * ratio;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}

internal enum ClassicInstallerProgressStage
{
    PreparingInstallation,
    CheckingExistingInstallation,
    DeployingApplicationFiles,
    RegisteringApplication,
    CreatingShortcuts,
    Completed
}

internal sealed class ClassicInstallerProgressChangedEventArgs(
    double progressPercentage,
    ClassicInstallerProgressStage stage,
    string? currentFileName) : EventArgs
{
    internal double ProgressPercentage { get; } = Math.Clamp(progressPercentage, 0, 100);

    internal ClassicInstallerProgressStage Stage { get; } = stage;

    internal string? CurrentFileName { get; } = currentFileName;
}
