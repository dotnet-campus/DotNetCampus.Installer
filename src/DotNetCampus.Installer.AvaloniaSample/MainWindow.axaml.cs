using Avalonia.Controls;
using Avalonia.Input;

using DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;
using DotNetCampus.Installer.AvaloniaSample.ViewModels;
using DotNetCampus.Installer.Lib.Hosts;

namespace DotNetCampus.Installer.AvaloniaSample;
public partial class MainWindow : Window
{
    public MainWindow(InstallerProgram installerProgram)
    {
        InstallerProgram = installerProgram;
        ViewModel = new MainViewModel(installerProgram);
        DataContext = ViewModel;
     
        InitializeComponent();
    }

    public MainViewModel ViewModel { get; }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
        base.OnPointerPressed(e);
    }

    public InstallerProgram InstallerProgram { get; }
}