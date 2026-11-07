using System;
using dotnetCampus.Configurations;
using System.ComponentModel;
using System.IO;
using Avalonia.Media.Imaging;
using Polygon = Avalonia.Controls.Shapes.Polygon;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;
using DotNetCampus.Installer.AvaloniaClassic.ViewModels;

namespace DotNetCampus.Installer.AvaloniaClassic;

/// <summary>安装向导的可见页面。</summary>
public enum InstallerPage { License, Options, Progress, Completed }

/// <summary>设计师版本的安装窗口，安装行为由共享的经典安装逻辑提供。</summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ResourceManager _strings = new("DotNetCampus.Installer.AvaloniaClassicLite.Resources.Strings", typeof(MainWindow).Assembly);
    private InstallerPage _page;
    private readonly bool _hasLicense;
    private readonly Bitmap? _productIcon;
    /// <summary>创建预览窗口。</summary>
    public MainWindow() : this(null) { }
    /// <summary>创建使用实际安装程序的窗口。</summary>
    public MainWindow(ClassicInstallerProgram? installerProgram)
    {
        _viewModel = new MainWindowViewModel(installerProgram);
        _hasLicense = installerProgram is null || !string.IsNullOrWhiteSpace(
            installerProgram.AppConfigurator.Of<ClassicInstallerConfiguration>().LicenseText);
        _viewModel.HasAcceptedLicense = !_hasLicense;
        _viewModel.ShouldLaunchApplication = true;
        InitializeComponent();
        var product = _viewModel.ProductName;
        Title = _viewModel.WindowTitle;
        TitleText.Text = Title;
        ProductText.Text = product;
        if (installerProgram is not null && !string.IsNullOrWhiteSpace(
            installerProgram.AppConfigurator.Of<ClassicInstallerConfiguration>().IconRelativePath))
        {
            var iconPath = Path.Join(installerProgram.StandardInstallContext.WorkingFolder.FullName, "InstallerIcon");
            _productIcon = new Bitmap(iconPath);
            Icon = new WindowIcon(iconPath);
        }
        SmallIcon.Content = CreateIcon(22, 23);
        ProductIcon.Content = CreateIcon(92, 92);
        LicenseTitle.IsVisible = false;
        LicenseText.Text = _viewModel.LicenseText;
        Agreement.Content = Text("Agreement");
        OptionsHeading.Text = Text("Options");
        ProgressHeading.Text = Text("Progress");
        CompletionHeading.Text = Text("Completed");
        LocationLabel.Text = Text("Location");
        ProcessingLabel.Text = Text("Processing");
        ProcessingText.Text = product;
        Browse.Content = Text("BrowseButton");
        OptionsButton.Content = Text("OptionsButton");
        InstallButton.Content = Text("InstallButton");
        OkButton.Content = Text("OkButton");
        CancelButton.Content = Text("CancelButton");
        Location.Text = _viewModel.InstallationFolder;
        InstallButton.IsEnabled = _viewModel.StartInstallationCommand.CanExecute(null);
        ShowPage(_hasLicense ? InstallerPage.License : InstallerPage.Options);
        _viewModel.CloseRequested += CloseRequested;
        _viewModel.PropertyChanged += ViewModelChanged;
        Loaded += (_, _) =>
        {
            CancelButton.Focus();
            if (installerProgram is not null && TryGetPlatformHandle() is { } handle)
                installerProgram.StandardInstallContext.InstallerUIWindowHandler = handle.Handle;
        };
        Closing += (_, e) =>
        {
            if (_viewModel.IsInstalling)
            {
                e.Cancel = true;
                _viewModel.CancelInstallationCommand.Execute(null);
            }
        };
        Closed += (_, _) =>
        {
            _viewModel.CloseRequested -= CloseRequested;
            _viewModel.PropertyChanged -= ViewModelChanged;
            _viewModel.Dispose();
            _productIcon?.Dispose();
        };
    }

    /// <summary>显示指定页面，不启动安装操作；可用于设计预览和视觉回归测试。</summary>
    public void ShowPage(InstallerPage page)
    {
        _page = page;
        LicensePage.IsVisible = page == InstallerPage.License;
        OptionsPage.IsVisible = page == InstallerPage.Options;
        ProgressPage.IsVisible = page == InstallerPage.Progress;
        CompletionHeading.IsVisible = page == InstallerPage.Completed;
        OptionsButton.IsVisible = page == InstallerPage.License;
        InstallButton.IsVisible = page == InstallerPage.License || (!_hasLicense && page == InstallerPage.Options);
        OkButton.IsVisible = _hasLicense && page == InstallerPage.Options;
        CancelButton.Content = Text(page == InstallerPage.Completed ? "CloseButton" : "CancelButton");
    }

    private string Text(string key) => _strings.GetString(key) ?? throw new MissingManifestResourceException(key);
    private void CloseRequested(object? sender, EventArgs e) => Close();
    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.InstallationProgress))
            ProgressFill.Width = 692 * _viewModel.InstallationProgress / 100;
        if (e.PropertyName == nameof(MainWindowViewModel.IsCompleted) && _viewModel.IsCompleted)
        {
            CompletionHeading.Text = _viewModel.InstallationSucceeded
                ? Text("Completed") : _viewModel.ProgressHeadline;
            ShowPage(InstallerPage.Completed);
        }
    }
    private void AgreementChanged(object? sender, RoutedEventArgs e)
    {
        _viewModel.HasAcceptedLicense = !_hasLicense || Agreement.IsChecked == true;
        InstallButton.IsEnabled = _viewModel.StartInstallationCommand.CanExecute(null);
    }
    private void OptionsClicked(object? sender, RoutedEventArgs e)
    {
        Location.Text = _viewModel.InstallationFolder;
        ShowPage(InstallerPage.Options);
        Location.Focus();
        Location.SelectAll();
    }
    private void OkClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Location.Text)) return;
        _viewModel.SetInstallationFolder(Location.Text);
        ShowPage(InstallerPage.License);
    }
    private async void BrowseClicked(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = Text("Location"), AllowMultiple = false });
        if (folders.FirstOrDefault()?.TryGetLocalPath() is { } path) Location.Text = path;
    }
    private void InstallClicked(object? sender, RoutedEventArgs e)
    {
        if (!_hasLicense && _page == InstallerPage.Options)
        {
            if (string.IsNullOrWhiteSpace(Location.Text)) return;
            _viewModel.SetInstallationFolder(Location.Text);
        }
        if (!_viewModel.StartInstallationCommand.CanExecute(null)) return;
        ShowPage(InstallerPage.Progress);
        _viewModel.StartInstallationCommand.Execute(null);
    }
    private void CancelClicked(object? sender, RoutedEventArgs e)
    {
        if (_hasLicense && _page == InstallerPage.Options) ShowPage(InstallerPage.License);
        else if (_viewModel.IsInstalling) _viewModel.CancelInstallationCommand.Execute(null);
        else Close();
    }
    private void MinimizeClicked(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void TitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }
    private Control CreateIcon(double width, double height)
    {
        if (_productIcon is not null)
            return new Image { Width = width, Height = height, Source = _productIcon, Stretch = Stretch.Uniform };

        // 与经典安装器 InstallerBrandingControl 的默认图形保持一致。
        var canvas = new Canvas { Width = 86, Height = 78 };
        AddFace("18,34 43,20 68,34 43,49", "#A8C9ED", 1.3);
        AddFace("18,34 43,49 43,69 18,54", "#8EB7E3", 1.3);
        AddFace("43,49 68,34 68,54 43,69", "#6F9FD0", 1.3);
        AddFace("31,18 43,11 55,18 43,25", "#D4E5F7", 1.2);
        AddFace("31,18 43,25 43,39 31,32", "#AECDEC", 1.2);
        AddFace("43,25 55,18 55,32 43,39", "#88B1DD", 1.2);
        return new Viewbox { Width = width, Height = height, Child = canvas };

        void AddFace(string points, string fill, double thickness) => canvas.Children.Add(new Polygon
        {
            Points = new Points(points.Split(' ').Select(pair =>
            {
                var coordinates = pair.Split(',');
                return new Point(int.Parse(coordinates[0]), int.Parse(coordinates[1]));
            })), Fill = Brush.Parse(fill),
            Stroke = Brush.Parse("#3974AE"), StrokeThickness = thickness
        });
    }
}
