namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms;

/// <summary>
/// 表示标准安装程序解压缩安装文件时的进度。
/// </summary>
public sealed record StandardInstallerDecompressProgress
{
    internal StandardInstallerDecompressProgress(
        string currentFileName,
        long decompressedBytes,
        long totalBytes)
    {
        CurrentFileName = currentFileName;
        DecompressedBytes = decompressedBytes;
        TotalBytes = totalBytes;
    }

    /// <summary>
    /// 获取当前正在解压缩的相对文件路径。
    /// </summary>
    public string CurrentFileName { get; }

    /// <summary>
    /// 获取已解压缩的载荷字节数，包括当前文件已完成的部分。
    /// </summary>
    public long DecompressedBytes { get; }

    /// <summary>
    /// 获取需要解压缩的载荷总字节数。
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// 获取解压缩完成百分比，取值范围为 0 到 100。
    /// </summary>
    public double ProgressPercentage => TotalBytes <= 0
        ? 100
        : Math.Clamp(DecompressedBytes / (double) TotalBytes * 100, 0, 100);
}
