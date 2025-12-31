using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

using DotNetCampus.Installer.AvaloniaSample.ViewModels;

using System;
using System.Globalization;

namespace DotNetCampus.Installer.AvaloniaSample.Views;

public partial class InstallFinishUserControl : UserControl
{
    public InstallFinishUserControl()
    {
        InitializeComponent();
    }

    private void FinishInstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OnFinish?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnFinish;
}

public class InstallStatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is InstallStatus installStatus)
        {
            return installStatus switch
            {
                InstallStatus.Finished => "安装完成",
                InstallStatus.Error => "安装错误",
                _ => "未知状态"
            };
        }

        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}