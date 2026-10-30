using dotnetCampus.Configurations;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>提供经典安装界面共用的安装流程。</summary>
public class ClassicInstallerProgram : StandardInstallerProgram
{
    /// <summary>创建使用指定安装上下文和配置的安装程序。</summary>
    public ClassicInstallerProgram(StandardInstallContext context, IAppConfigurator appConfigurator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(appConfigurator);
        StandardInstallContext = context;
        AppConfigurator = appConfigurator;
        DeleteFolderDelayUntilReboot(context.WorkingFolder);
    }

    public IAppConfigurator AppConfigurator { get; }
    public override StandardInstallContext StandardInstallContext { get; }
    public event EventHandler<ClassicInstallerProgressChangedEventArgs>? ProgressChanged;
    public bool CanLaunchApplication => !string.IsNullOrWhiteSpace(StandardInstallContext.LauncherExeRelativePath);
    public override Task InstallAsync() => InstallAsync(CancellationToken.None);

    /// <summary>执行安装，并在各安装步骤之间响应取消。</summary>
    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        Logger.WriteLog($"Install start. Installer PID={Environment.ProcessId}");
        ReportProgress(0, ClassicInstallerProgressStage.PreparingInstallation);
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(8, ClassicInstallerProgressStage.CheckingExistingInstallation);
        ClearOldVersion();
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(StandardInstallContext.MainInstallPath);
        Logger.SetLogFile(new FileInfo(Path.Join(StandardInstallContext.MainInstallPath, "InstallerLog.txt")));
        ReportProgress(12, ClassicInstallerProgressStage.DeployingApplicationFiles);
        await Decompress(new InlineProgress<StandardInstallerDecompressProgress>(progress =>
            ReportProgress(12 + 78 * Math.Clamp(progress.ProgressPercentage / 100, 0, 1),
                ClassicInstallerProgressStage.DeployingApplicationFiles, progress.CurrentFileName)), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(95, ClassicInstallerProgressStage.RegisteringApplication);
        WriteRegister();
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(98, ClassicInstallerProgressStage.CreatingShortcuts);
        CreateShortcut();
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(100, ClassicInstallerProgressStage.Completed);
    }

    /// <summary>尝试启动已安装的应用程序。</summary>
    public bool TryLaunchApplication()
    {
        var path = StandardInstallContext.GetLauncherExeFullPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Logger.WriteLog($"Can not launch application. Launcher path='{path}'.");
            return false;
        }
        return StartProcessWithShellProcessToken(path);
    }

    private void ReportProgress(double progress, ClassicInstallerProgressStage stage, string? file = null) =>
        ProgressChanged?.Invoke(this, new ClassicInstallerProgressChangedEventArgs(progress, stage, file));

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}

/// <summary>安装流程阶段。</summary>
public enum ClassicInstallerProgressStage
{
    PreparingInstallation, CheckingExistingInstallation, DeployingApplicationFiles,
    RegisteringApplication, CreatingShortcuts, Completed
}

/// <summary>安装流程进度。</summary>
public sealed class ClassicInstallerProgressChangedEventArgs(double progressPercentage,
    ClassicInstallerProgressStage stage, string? currentFileName) : EventArgs
{
    public double ProgressPercentage { get; } = Math.Clamp(progressPercentage, 0, 100);
    public ClassicInstallerProgressStage Stage { get; } = stage;
    public string? CurrentFileName { get; } = currentFileName;
}
