using System.Reflection.PortableExecutable;

namespace DotNetCampus.Installer.Lib.Utils.PEOverlays;

public class PEOverlayContentWriter
{
    public async Task WriteOverlayInstallerContentAsync(FileInfo peFile, Stream contentStream)
    {
        await using var fileStream = new FileStream(peFile.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var peReader = new PEReader(fileStream, PEStreamOptions.LeaveOpen);
        var info = PEOverlayContentHelper.GetOverlayAndCertificate(peReader, fileStream.Length);
        var range = info.OverlayBeforeCertificate;
        if (range.Length != 0)
        {
            throw new InvalidOperationException($"已经存在 Overlay 内容");
        }

        // 移动到文件末尾，准备写入 Overlay 内容
        fileStream.Seek(0, SeekOrigin.End);

        // 先写入头部
        var header = PEOverlayHeader.ContentOfInstallerOverlayHeader;
        fileStream.Write(header);
        // 再写入内容长度
        var lengthBuffer = BitConverter.GetBytes(contentStream.Length);
        fileStream.Write(lengthBuffer);
        // 最后写入内容
        await contentStream.CopyToAsync(fileStream);
    }
}