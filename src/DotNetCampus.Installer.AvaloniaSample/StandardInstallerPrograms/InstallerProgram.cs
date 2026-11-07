using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;
using Microsoft.DotNet.Archive;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

/// <summary>
/// 安装程序
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public class InstallerProgram : StandardInstallerProgram
{
    private const double InstallPreparationProgress = 8;
    private const double DecompressStartProgress = 12;
    private const double DecompressEndProgress = 90;
    private const double RegisterProgress = 95;
    private const double ShortcutProgress = 98;

    public InstallerProgram()
    {
        StandardInstallContext = InstallContextBuilder.Build();
    }

    public override StandardInstallContext StandardInstallContext { get; }

    public event EventHandler<InstallerProgressChangedEventArgs>? ProgressChanged;

    public override async Task InstallAsync()
    {
        Logger.WriteLog($"Install start. Installer PID={Environment.ProcessId}");

        ReportProgress(0, "准备安装环境", "正在初始化安装参数");

        ReportProgress(InstallPreparationProgress, "检查旧版本", "正在清理旧版本和运行中的进程");
        ClearOldVersion();

        Directory.CreateDirectory(StandardInstallContext.MainInstallPath);
        var logFile = new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, "InstallerLog.txt"));
        Logger.SetLogFile(logFile);

        ReportProgress(DecompressStartProgress, "部署安装文件", $"正在释放文件到 {StandardInstallContext.MainInstallPath}");
        await Decompress();

        ReportProgress(RegisterProgress, "写入系统信息", "正在注册安装信息");
        WriteRegister();

        ReportProgress(ShortcutProgress, "创建快捷方式", "正在创建桌面和开始菜单入口");
        CreateShortcut();

        ReportProgress(100, "安装完成", $"{StandardInstallContext.DisplayProductName} 已可使用");
    }

    public override async Task Decompress()
    {
        long totalPayloadBytes = 0;
        long completedPayloadBytes = 0;

        var overlayDirectoryArchive = await StandardInstallContext.GetOverlayDirectoryArchive();
        List<IDirectoryArchiveEntryFile>? overlayEntryList = null;

        if (overlayDirectoryArchive is not null)
        {
            overlayEntryList = overlayDirectoryArchive.EntryFileList
                .Where(file => file.RelativePath.RelativePath.StartsWith(@"Packing\", StringComparison.OrdinalIgnoreCase))
                .Select(static file => (IDirectoryArchiveEntryFile) file)
                .ToList();

            if (overlayEntryList.Count == 0)
            {
                throw new InvalidOperationException();
            }

            totalPayloadBytes += overlayEntryList.Sum(file => file.OriginFileLength);
        }

        var contentResourceAssetsInfo = StandardInstallContext.ContentResourceAssetsInfo;
        if (contentResourceAssetsInfo is null)
        {
            throw new InvalidOperationException();
        }

        await using var contentStream = contentResourceAssetsInfo.Value.GetManifestResourceStream();
        var embeddedDirectoryArchive = await DirectoryArchive.OpenReadAsync(contentStream);
        var embeddedEntryList = embeddedDirectoryArchive.EntryFileList
            .Select(static file => (IDirectoryArchiveEntryFile) file)
            .ToList();

        totalPayloadBytes += embeddedEntryList.Sum(file => file.OriginFileLength);

        var installDirectory = Directory.CreateDirectory(StandardInstallContext.MainInstallPath);

        if (overlayEntryList is not null)
        {
            Logger.WriteLog($"Decompress from Overlay to '{StandardInstallContext.MainInstallPath}'");
            completedPayloadBytes = await ExtractEntriesAsync(
                overlayEntryList,
                installDirectory,
                static relativePath => relativePath[@"Packing\".Length..],
                completedPayloadBytes,
                totalPayloadBytes);
        }

        Logger.WriteLog($"Decompress embedded content to '{StandardInstallContext.MainInstallPath}'");
        _ = await ExtractEntriesAsync(
            embeddedEntryList,
            installDirectory,
            static relativePath => relativePath,
            completedPayloadBytes,
            totalPayloadBytes);
    }

    private async Task<long> ExtractEntriesAsync(
        IReadOnlyList<IDirectoryArchiveEntryFile> entryList,
        DirectoryInfo installDirectory,
        Func<string, string> relativePathTransform,
        long completedPayloadBytes,
        long totalPayloadBytes)
    {
        foreach (var entry in entryList)
        {
            var relativePath = relativePathTransform(entry.RelativePath.RelativePath);
            var outputFile = new FileInfo(Path.Join(installDirectory.FullName, relativePath));
            outputFile.Directory?.Create();

            var progress = new InlineProgress<ProgressReport>(report =>
            {
                var percentage = CalculateProgress(completedPayloadBytes + report.Ticks, totalPayloadBytes);
                ReportProgress(percentage, "部署安装文件", $"正在写入 {relativePath}", relativePath);
            });

            ReportProgress(CalculateProgress(completedPayloadBytes, totalPayloadBytes), "部署安装文件", $"正在写入 {relativePath}", relativePath);
            await entry.SaveToFileAsync(outputFile, progress);

            completedPayloadBytes += entry.OriginFileLength;
            ReportProgress(CalculateProgress(completedPayloadBytes, totalPayloadBytes), "部署安装文件", $"已写入 {relativePath}", relativePath);
        }

        return completedPayloadBytes;
    }

    private void ReportProgress(double progressPercentage, string stageText, string detailText, string? currentFileName = null)
    {
        ProgressChanged?.Invoke(this, new InstallerProgressChangedEventArgs(progressPercentage, stageText, detailText, currentFileName));
    }

    private static double CalculateProgress(long completedPayloadBytes, long totalPayloadBytes)
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