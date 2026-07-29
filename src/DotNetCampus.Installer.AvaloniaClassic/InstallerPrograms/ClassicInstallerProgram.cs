using dotnetCampus.Configurations;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;
using System;
using System.IO;
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
        var decompressProgress = new InlineProgress<StandardInstallerDecompressProgress>(progress =>
        {
            ReportProgress(
                CalculateDecompressProgress(progress.ProgressPercentage),
                ClassicInstallerProgressStage.DeployingApplicationFiles,
                progress.CurrentFileName);
        });
        await Decompress(decompressProgress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(RegisterProgress, ClassicInstallerProgressStage.RegisteringApplication);
        WriteRegister();
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(ShortcutProgress, ClassicInstallerProgressStage.CreatingShortcuts);
        CreateShortcut();
        cancellationToken.ThrowIfCancellationRequested();

        ReportProgress(100, ClassicInstallerProgressStage.Completed);
    }

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

    private void ReportProgress(
        double progressPercentage,
        ClassicInstallerProgressStage stage,
        string? currentFileName = null)
    {
        ProgressChanged?.Invoke(
            this,
            new ClassicInstallerProgressChangedEventArgs(progressPercentage, stage, currentFileName));
    }

    private static double CalculateDecompressProgress(double decompressProgressPercentage) =>
        DecompressStartProgress
        + (DecompressEndProgress - DecompressStartProgress)
        * Math.Clamp(decompressProgressPercentage / 100, 0, 1);

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
