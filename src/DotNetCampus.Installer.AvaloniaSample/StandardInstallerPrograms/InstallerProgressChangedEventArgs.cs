using System;

namespace DotNetCampus.Installer.AvaloniaSample.StandardInstallerPrograms;

public sealed class InstallerProgressChangedEventArgs : EventArgs
{
    public InstallerProgressChangedEventArgs(double progressPercentage, string stageText, string detailText, string? currentFileName = null)
    {
        StageText = string.IsNullOrWhiteSpace(stageText) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(stageText)) : stageText;
        DetailText = string.IsNullOrWhiteSpace(detailText) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(detailText)) : detailText;
        CurrentFileName = currentFileName;
        ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
    }

    public double ProgressPercentage { get; }

    public string StageText { get; }

    public string DetailText { get; }

    public string? CurrentFileName { get; }
}
