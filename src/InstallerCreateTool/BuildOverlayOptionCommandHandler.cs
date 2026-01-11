using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using DotNetCampus.Installer.Lib.Utils.PEOverlays;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerCreateTool;

/// <summary>
/// 存放构建为 PE 的 overlay 内容的命令行处理器
/// </summary>
[Command("build overlay")]
internal class BuildOverlayOptionCommandHandler : ICommandHandler
{
    /// <summary>
    /// 是否强行使用 UTF-8 编码作为控制台输出
    /// </summary>
    [Option()]
    public bool? ForceUtf8ConsoleOutput { get; init; }

    /// <summary>
    /// 安装器的 PE 文件路径，将在此文件中写入 Overlay 内容
    /// </summary>
    [Option("Installer")]
    public required string InstallerFile { get; init; }

    [Option("File")]
    public string[]? FileList { get; init; }

    [Option("Folder")]
    public string[]? FolderList { get; init; }

    [Option()]
    public string? WorkingFolder { get; init; }

    public async Task<int> RunAsync()
    {
        var workingFolder = WorkingFolder;
        if (string.IsNullOrEmpty(workingFolder))
        {
            workingFolder = Path.Join(Path.GetTempPath(), $"InstallerCreateTool_{Path.GetRandomFileName()}");
        }

        Directory.CreateDirectory(workingFolder);
        await using var archiveFile = new FileStream(Path.Join(workingFolder, $"{Path.GetRandomFileName()}.assets"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096,
            // 关闭就删除，不用持续占用磁盘。用这个文件仅仅只是为了避免大量占用内存而已
            FileOptions.DeleteOnClose
            // 异步操作
            | FileOptions.Asynchronous
            // 顺序扫描，大部分情况下都是顺序读写，设置这个选项可以提升性能
            | FileOptions.SequentialScan);

        var fileList = new List<DirectoryArchiveFileInfo>();
        if (FileList is not null)
        {
            foreach (var file in FileList)
            {
                var fileInfo = new FileInfo(file);
                fileList.Add(new DirectoryArchiveFileInfo(fileInfo.Name, fileInfo));
            }
        }

        if (FolderList is not null)
        {
            foreach (var folder in FolderList)
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(folder, file);
                    var fileInfo = new FileInfo(file);
                    fileList.Add(new DirectoryArchiveFileInfo(relativePath, fileInfo));
                }
            }
        }

        var compressWorkingFolder = Directory.CreateDirectory(Path.Join(workingFolder, "Compress"));
        await DirectoryArchive.CompressAsync(fileList, archiveFile, compressWorkingFolder);

        // 压缩完成之后，写入到 PE 文件里面
        var writer = new PEOverlayContentWriter();
        archiveFile.Seek(0, SeekOrigin.Begin);
        await writer.WriteOverlayInstallerContentAsync(new FileInfo(InstallerFile), archiveFile);

        return 0;
    }
}
