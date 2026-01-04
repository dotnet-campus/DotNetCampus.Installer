using Microsoft.DotNet.Archive;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public interface IDirectoryArchiveEntryFile
{
    /// <summary>
    /// 相对的路径，可以认为是文件名
    /// </summary>
    string RelativePath { get; }

    /// <summary>
    /// 解压缩后拷贝到目标流中
    /// </summary>
    /// <param name="destinationStream"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    Task CopyToAsync(Stream destinationStream, IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// 保存到文件里
    /// </summary>
    /// <param name="outputFile"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    Task SaveToFileAsync(FileInfo outputFile, IProgress<ProgressReport>? progress = null);
}