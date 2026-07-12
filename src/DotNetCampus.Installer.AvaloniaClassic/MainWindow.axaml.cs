using System.Linq;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using DotNetCampus.Installer.AvaloniaClassic.ViewModels;

namespace DotNetCampus.Installer.AvaloniaClassic;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        InitializeComponent();

        _viewModel.CloseRequested += (_, _) => Close();
        _viewModel.BrowseInstallationFolderRequested += async (_, _) => await BrowseInstallationFolderAsync();
        Closed += (_, _) => _viewModel.Dispose();
    }

    private async System.Threading.Tasks.Task BrowseInstallationFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Installation Folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is not null)
        {
            _viewModel.SetInstallationFolder(folder.Path.LocalPath);
        }
    }
}
