using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DotNetCampus.Installer.Lib.StandardInstallerPrograms;

namespace DotNetCampus.Installer.AvaloniaClassicSimple;

/// <summary>经典简洁版安装窗口。</summary>
public partial class MainWindow : Window
{
    private readonly InstallerViewModel _model;
    private readonly ClassicInstallerProgram? _program;
    private CancellationTokenSource? _cancellation;
    private bool _closeAfterCancellation;

    /// <summary>创建指定状态和可选真实安装内核的窗口。</summary>
    public MainWindow(InstallerViewModel model, ClassicInstallerProgram? program = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _program = program;
        InitializeComponent();
        DataContext = model;
        Loaded += (_, _) =>
        {
            if (program is not null && TryGetPlatformHandle() is { } handle)
                program.StandardInstallContext.InstallerUIWindowHandler = handle.Handle;
        };
        Closing += (_, e) =>
        {
            if (_cancellation is null) return;
            e.Cancel = true;
            _closeAfterCancellation = true;
            _cancellation.Cancel();
        };
    }

    private void OptionsClicked(object? sender, RoutedEventArgs e) => _model.OpenOptions();
    private void OkClicked(object? sender, RoutedEventArgs e) => _model.CommitOptions();
    private void CancelClicked(object? sender, RoutedEventArgs e)
    {
        if (_model.IsOptions) _model.CancelOptions();
        else if (_cancellation is not null) _cancellation.Cancel();
        else Close();
    }

    private async void BrowseClicked(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        { Title = _model.LocationText, AllowMultiple = false });
        if (folders.FirstOrDefault()?.TryGetLocalPath() is { } path) _model.DraftPath = path;
    }

    private async void InstallClicked(object? sender, RoutedEventArgs e) => await InstallAsync();

    /// <summary>运行真实安装流程；没有内核的预览窗口不会模拟成功。</summary>
    public async Task InstallAsync()
    {
        if (_program is null || !_model.BeginInstallation()) return;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        _program.StandardInstallContext.InstallRootPath = _model.InstallationPath;
        _program.ProgressChanged += ProgressChanged;
        try
        {
            await Task.Run(() => _program.InstallAsync(cancellation.Token), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _model.CompleteInstallation();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _model.CompleteInstallation(cancelled: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or DotNetCampus.Installer.Lib.Exceptions.InstallerException or InvalidOperationException)
        {
            _program.Logger.WriteLog(exception.ToString());
            _model.CompleteInstallation(exception.Message);
        }
        finally
        {
            _program.ProgressChanged -= ProgressChanged;
            _cancellation = null;
        }
        if (_closeAfterCancellation) Close();
    }

    private void ProgressChanged(object? sender, ClassicInstallerProgressChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => _model.ReportProgress(e));
}
