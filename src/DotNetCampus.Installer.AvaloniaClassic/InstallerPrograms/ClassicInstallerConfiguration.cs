using dotnetCampus.Configurations;

namespace DotNetCampus.Installer.AvaloniaClassic.InstallerPrograms;

public sealed class ClassicInstallerConfiguration() : Configuration("ClassicInstaller")
{
    public string? WindowTitle
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? WelcomeText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? PreparationHeadline
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? PreparationDescription
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? LicenseText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? LicenseAgreementText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationFolderLabel
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? BrowseInstallationFolderTitle
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? BrowseButtonText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? NextButtonText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? CancelButtonText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? FinishButtonText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? LaunchApplicationText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallingHeadline
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? PreparingInstallationStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? PreparingInstallationDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? CheckingExistingInstallationStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? CheckingExistingInstallationDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? DeployingApplicationFilesStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? DeployingApplicationFilesDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? RegisteringApplicationStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? RegisteringApplicationDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? CreatingShortcutsStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? CreatingShortcutsDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationCompleteHeadline
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationSucceededStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationCompleteDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationFailedHeadline
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationFailedStepText
    {
        get => GetString();
        set => SetValue(value);
    }

    public string? InstallationFailedDetailText
    {
        get => GetString();
        set => SetValue(value);
    }

    public bool? HasAcceptedLicenseByDefault
    {
        get => GetBoolean();
        set => SetValue(value);
    }

    public bool? ShouldLaunchApplicationByDefault
    {
        get => GetBoolean();
        set => SetValue(value);
    }
}