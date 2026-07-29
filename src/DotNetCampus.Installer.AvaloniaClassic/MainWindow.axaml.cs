using Avalonia.Controls;
using Avalonia.Platform.Storage;

using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;
using DotNetCampus.Installer.AvaloniaClassic.ViewModels;

using System.Linq;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class MainWindow : Window
{
    public MainWindow():this(null)
    {
    }

    public MainWindow(ClassicInstallerProgram? installerProgram)
    {
        _viewModel = new MainWindowViewModel(installerProgram);
        DataContext = _viewModel;
        InitializeComponent();

        Loaded += (_, _) => SetInstallerWindowHandle(installerProgram);
        _viewModel.CloseRequested += (_, _) => Close();
        _viewModel.BrowseInstallationFolderRequested += async (_, _) => await BrowseInstallationFolderAsync();
        Closed += (_, _) => _viewModel.Dispose();
    }

    private readonly MainWindowViewModel _viewModel;

    private async System.Threading.Tasks.Task BrowseInstallationFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _viewModel.BrowseInstallationFolderTitle,
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is not null)
        {
            _viewModel.SetInstallationFolder(folder.Path.LocalPath);
        }
    }

    private void SetInstallerWindowHandle(ClassicInstallerProgram? installerProgram)
    {
        if (installerProgram is not null && TryGetPlatformHandle() is { } handle)
        {
            installerProgram.StandardInstallContext.InstallerUIWindowHandler = handle.Handle;
        }
    }
}
