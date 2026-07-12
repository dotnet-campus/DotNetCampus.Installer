using Avalonia.Controls;

namespace DotNetCampus.Installer.AvaloniaClassic.Views;

public partial class InstallProgressControl : UserControl
{
    public InstallProgressControl()
    {
        InitializeComponent();
    }

    public void ShowInstallingState()
    {
        HeadlineTextBlock.Text = "安装正在进行中...";
        InstallProgressBar.Value = 64;
        CurrentStepTextBlock.Text = "正在解压文件...";
        RegisterComponentTextBlock.Text = "• 正在注册组件...";
        DesktopShortcutTextBlock.Text = "• 正在创建桌面快捷方式...";
        StartMenuShortcutTextBlock.Text = "• 正在创建开始菜单快捷方式...";
        RemainingTimeTextBlock.Text = "预计剩余时间：3秒";
    }

    public void ShowCompletedState()
    {
        HeadlineTextBlock.Text = "安装已完成";
        InstallProgressBar.Value = 100;
        CurrentStepTextBlock.Text = "软件已成功安装。";
        RegisterComponentTextBlock.Text = "• 组件注册完成";
        DesktopShortcutTextBlock.Text = "• 桌面快捷方式创建完成";
        StartMenuShortcutTextBlock.Text = "• 开始菜单快捷方式创建完成";
        RemainingTimeTextBlock.Text = "点击“完成”退出安装向导。";
    }
}
