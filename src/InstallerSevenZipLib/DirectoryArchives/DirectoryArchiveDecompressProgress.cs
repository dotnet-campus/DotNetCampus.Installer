using System.Diagnostics;
using Microsoft.DotNet.Archive;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 解压缩的进度条
/// </summary>
public class DirectoryArchiveDecompressProgress
{
    public DirectoryArchiveDecompressProgress()
    {
        Progress = new InnerProgress(this);
    }

    internal DirectoryArchiveDecompressProgress(bool shouldIgnore)
    {
        ShouldIgnore = shouldIgnore;

        if (shouldIgnore)
        {
            Progress = new NoneProgressReport();
        }
        else
        {
            Progress = new InnerProgress(this);
        }
    }

    /// <summary>
    /// 更新的频率
    /// </summary>
    public TimeSpan UpdateFrequency { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 是否应该被忽略掉，不进行任何进度更新
    /// </summary>
    internal bool ShouldIgnore { get; }

    /// <summary>
    /// 当前正在解压缩的路径
    /// </summary>
    public string? CurrentDecompressedPath { get; private set; }

    /// <summary>
    /// 当前正在解压缩的文件的百分比进度条，为 0 - 100 之间的数值
    /// </summary>
    public double CurrentDecompressedProgressPercentage { get; private set; }

    /// <summary>
    /// 百分比进度条，为 0 - 100 之间的数值
    /// </summary>
    public double TotalProgressPercentage { get; private set; }

    /// <summary>
    /// 解压缩的文件数量
    /// </summary>
    public int DecompressedFileCount { get; private set; }

    /// <summary>
    /// 文件总数量
    /// </summary>
    public int TotalFileCount { get; private set; }

    /// <summary>
    /// 是否已经完成解压缩
    /// </summary>
    public bool IsFinished { get; private set; }

    public event EventHandler<DirectoryArchiveDecompressProgress>? Updated; 

    private IProgress<ProgressReport> Progress { get; }

    internal void Start(int totalFileCount)
    {
        TotalFileCount = totalFileCount;
    }

    internal IProgress<ProgressReport> UpdateCurrentDecompress(string currentDecompressedPath)
    {
        CurrentDecompressedProgressPercentage = 0;
        CurrentDecompressedPath = currentDecompressedPath;
        Updated?.Invoke(this, this);
        return Progress;
    }

    internal void SetCurrentDecompressFinish()
    {
        DecompressedFileCount++;
        TotalProgressPercentage = (double) DecompressedFileCount / TotalFileCount * 100;
        CurrentDecompressedProgressPercentage = 100;
        Updated?.Invoke(this, this);
    }

    internal void Finish()
    {
        Debug.Assert(DecompressedFileCount == TotalFileCount);
        CurrentDecompressedPath = null;
        CurrentDecompressedProgressPercentage = 0;
        TotalProgressPercentage = 100;
        IsFinished = true;
        Updated?.Invoke(this, this);
    }

    class InnerProgress : IProgress<ProgressReport>
    {
        public InnerProgress(DirectoryArchiveDecompressProgress progress)
        {
            _progress = progress;
        }

        private readonly DirectoryArchiveDecompressProgress _progress;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public void Report(ProgressReport value)
        {
            if (_stopwatch.Elapsed < _progress.UpdateFrequency)
            {
                // 频率限制为每秒一次
                return;
            }

            double current = value.Ticks / (double)value.Total;
            _progress.CurrentDecompressedProgressPercentage = current * 100;

            var total = _progress.TotalFileCount;
            var decompressed = _progress.DecompressedFileCount;

            // 计算总的进度百分比：
            // 已经解压缩的文件 + 当前文件的进度
            // 当前文件的进度最大值为 1 的值，取占总数为以下计算方法
            // 比如有 10 个文件，已经解压缩了 5 个文件，当前文件进度为百分之五十，则总进度为：
            // (5 / 10) + (0.5 / 10) = 0.55
            // 最终转换为百分比就是百分之五十五的值
            _progress.TotalProgressPercentage = ((double) decompressed / total + current / total) * 100 ;

            _progress.Updated?.Invoke(_progress, _progress);

            _stopwatch.Restart();
        }
    }
}