using System;
using System.ComponentModel;
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
    /// <summary>创建预览窗口。</summary>
    public MainWindow() : this(null) { }
    /// <summary>创建使用实际安装程序的窗口。</summary>
    public MainWindow(ClassicInstallerProgram? installerProgram)
    {
        _viewModel = new MainWindowViewModel(installerProgram);
        _viewModel.HasAcceptedLicense = false;
        _viewModel.ShouldLaunchApplication = true;
        InitializeComponent();
        var product = _viewModel.ProductName;
        Title = _viewModel.WindowTitle;
        TitleText.Text = Title;
        ProductText.Text = product;
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
        OptionsButton.IsVisible = InstallButton.IsVisible = page == InstallerPage.License;
        OkButton.IsVisible = page == InstallerPage.Options;
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
            CompletionHeading.Text = _viewModel.ProgressHeadline == LocalizedText.Current.InstallationCompleteHeadline
                ? Text("Completed") : _viewModel.ProgressHeadline;
            ShowPage(InstallerPage.Completed);
        }
    }
    private void AgreementChanged(object? sender, RoutedEventArgs e)
    {
        _viewModel.HasAcceptedLicense = Agreement.IsChecked == true;
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
        if (!_viewModel.StartInstallationCommand.CanExecute(null)) return;
        ShowPage(InstallerPage.Progress);
        _viewModel.StartInstallationCommand.Execute(null);
    }
    private void CancelClicked(object? sender, RoutedEventArgs e)
    {
        if (_page == InstallerPage.Options) ShowPage(InstallerPage.License);
        else if (_viewModel.IsInstalling) _viewModel.CancelInstallationCommand.Execute(null);
        else Close();
    }
    private void MinimizeClicked(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void TitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }
    private static Control CreateIcon(double width, double height)
    {
        var canvas = new Canvas { Width = 92, Height = 92 };
        canvas.Children.Add(new Border { Width = 90, Height = 90, Background = Brush.Parse("#D6CBAA"), BorderBrush = Brush.Parse("#EEE5CF"), BorderThickness = new Thickness(4), CornerRadius = new CornerRadius(2) });
        var face = new Border { Width = 73, Height = 74, Background = Brush.Parse("#606361"), BorderBrush = Brush.Parse("#A19D8C"), BorderThickness = new Thickness(3) };
        Canvas.SetLeft(face, 9); Canvas.SetTop(face, 9); canvas.Children.Add(face);
        string[] colors = ["#F26810", "#FFE419", "#76D216", "#12AFDF"];
        for (var i = 0; i < colors.Length; i++)
        {
            var stripe = new Border { Width = 15, Height = 40, Background = Brush.Parse(colors[i]) };
            Canvas.SetLeft(stripe, 16 + i * 15); Canvas.SetTop(stripe, 16); canvas.Children.Add(stripe);
        }
        for (var i = 0; i < 3; i++)
        {
            var key = new Border { Width = 13, Height = 13, Background = Brush.Parse("#E9DFC2"), BorderBrush = Brush.Parse("#ADA890"), BorderThickness = new Thickness(1) };
            Canvas.SetLeft(key, 16 + i * 24); Canvas.SetTop(key, 65); canvas.Children.Add(key);
        }
        return new Viewbox { Width = width, Height = height, Child = canvas };
    }
}
