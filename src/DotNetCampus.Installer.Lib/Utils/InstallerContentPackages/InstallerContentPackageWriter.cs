using System.Text;

namespace DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

/// <summary>
/// 安装包内容包的写入器
/// </summary>
/// 通常这是给安装包制作程序使用的。安装包安装过程不会用到这个类。这也是分开为两个类的原因
public class InstallerContentPackageWriter
{
    public static async Task WriteAsync(IReadOnlyList<ContentPackageFileInfo> files, Stream outputStream)
    {
        // 写入 Header
        var header = InstallerContentPackage.ContentPackageHeader;
        outputStream.Write(header);

        // 准备 FileBlock 列表
        var fileBlocks = new List<ContentPackageFileBlock>(files.Count);
        long currentOffset = 0;
        int totalLength = 0;

        // 文件块数量字段
        const int lengthOfFileBlockCountField = sizeof(int);
        totalLength += lengthOfFileBlockCountField;

        foreach (var fileInfo in files)
        {
            var file = fileInfo.File;

            var fileBlock = new ContentPackageFileBlock
            {
                RelativePath = fileInfo.RelativePath,
                FileLength = file.Length,
                FileContentOffset = currentOffset
            };
            fileBlocks.Add(fileBlock);
            currentOffset += file.Length;

            totalLength += fileBlock.FileBlockLength + sizeof(int); // + sizeof(int) 是 FileBlockLength 字段的长度
        }

        // 写入 FileBlock 列表
        await using (var binaryWriter = new BinaryWriter(outputStream, Encoding.UTF8, leaveOpen: true))
        {
            // 写入总长度
            binaryWriter.Write(totalLength);
            // 写入文件块数量
            binaryWriter.Write(fileBlocks.Count);

            foreach (var fileBlock in fileBlocks)
            {
                // - FileBlockLength: Int32
                // - FileNameLength: Int32
                // - FileName: String
                // - FileContentOffset: Int64
                // - FileLength: Int64
                binaryWriter.Write(fileBlock.FileBlockLength);
                binaryWriter.Write(fileBlock.RelativePathLength);
                var fileNameBytes = Encoding.UTF8.GetBytes(fileBlock.RelativePath);
                binaryWriter.Write(fileNameBytes);
                binaryWriter.Write(fileBlock.FileContentOffset);
                binaryWriter.Write(fileBlock.FileLength);
            }
        }

        // 写入文件内容
        foreach (var file in files)
        {
            var fileInfo = file.File;
            await using var fileStream = fileInfo.OpenRead();
            await fileStream.CopyToAsync(outputStream);
        }
    }
}