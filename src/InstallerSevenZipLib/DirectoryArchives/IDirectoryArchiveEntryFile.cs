using Microsoft.DotNet.Archive;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public interface IDirectoryArchiveEntryFile
{
    /// <summary>
    /// 相对的路径，可以认为是文件名
    /// </summary>
    /// 由于这是主要用于安装包的，也就不怕 `..\` 之类的路径穿越投毒问题
    /// 什么是路径穿越投毒？那就是在相对路径里面使用 `..\` 之类的路径，导致解压缩到不该解压缩的位置，从而覆盖系统文件等危险操作。在正常压缩软件里面，是应该拦截这种路径的，但是在安装包场景下，这种情况一般不会出现，即安装的包含内容都是由开发者完全控制的，开发者自己想不开想做这样的事情，那也只好顺着他的想法
    DirectoryArchiveEntryRelativePath RelativePath { get; }

    /// <summary>
    /// 压缩后的文件长度
    /// </summary>
    long CompressedFileLength { get; }

    /// <summary>
    /// 原始文件长度，未压缩前的长度
    /// </summary>
    long OriginFileLength { get; }

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