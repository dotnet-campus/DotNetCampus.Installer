using DotNetCampus.Installer.Lib.Utils.InstallerArrayPools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

namespace DotNetCampus.Installer.Lib.Utils.PEOverlays;

/// <summary>
/// 读取 PE 文件 Overlay 部分的安装器内容
/// </summary>
public class PEOverlayContentReader
{
    public async Task<OverlayInstallerContentInfo?> ReadOverlayInstallerContent(FileInfo peFile)
    {
        var fileStream = new FileStream(peFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        try
        {
            long maxLength = int.MaxValue - short.MaxValue;
            maxLength = Math.Min(fileStream.Length, maxLength);
            // 由于 PE 只能接收 int.MaxValue 大小的流，所以这里做一个限制
            await using var inputStream = new SliceStream(fileStream, 0, maxLength, leaveOpen: true);

            using var peReader = new PEReader(inputStream, PEStreamOptions.LeaveOpen);

            var info = PEOverlayContentHelper.GetOverlayAndCertificate(peReader, fileStream.Length);
            var range = info.OverlayBeforeCertificate;

            var headLength = PEOverlayHeader.ContentOfInstallerOverlayHeader.Length;
            if (range.Length < headLength)
            {
                // 没有安装器内容
                return null;
            }

            // 读取 Overlay 头部，判断是否是安装器内容
            fileStream.Seek(range.Offset, SeekOrigin.Begin);
            using var headerBuffer = InstallerArrayPool.Rent<byte>(headLength);
            var readBytes = await fileStream.ReadAsync(headerBuffer.Memory);
            if (readBytes != headLength)
            {
                throw new InvalidOperationException("读取 Overlay 头部失败，读取的字节数不足");
            }

            if (!headerBuffer.Span.SequenceEqual(PEOverlayHeader.ContentOfInstallerOverlayHeader))
            {
                // 不是安装器内容
                return null;
            }

            // 再读取 Length 长度，获取内容的长度
            var sizeOfLength = sizeof(long);
            var lengthBuffer = headerBuffer.Slice(0, sizeOfLength);
            readBytes = await fileStream.ReadAsync(lengthBuffer.Memory);
            if (readBytes != sizeOfLength)
            {
                throw new InvalidOperationException("读取 Overlay 内容长度失败，读取的字节数不足");
            }

            long contentLength = BitConverter.ToInt64(lengthBuffer.Span);
            if (contentLength <= 0 || contentLength > range.Length - headLength)
            {
                throw new InvalidOperationException("Overlay 内容长度不合法");
            }

            // 读取内容
            var startPosition = range.Offset + headLength + sizeOfLength;
            var sliceStream = new SliceStream(fileStream, startPosition, contentLength);
            return new OverlayInstallerContentInfo()
            {
                ContentStream = sliceStream,
                PEFile = peFile,
            };
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            // 要是出现异常了，那就应该释放，防止文件句柄泄漏
            // 如果没有异常，则交给上层业务去处理
            await fileStream.DisposeAsync();
            throw;
        }
    }
}

readonly record struct PEFileRange(long Offset, long Length);

readonly record struct OverlayInfo()
{
    public PEFileRange Certificate { get; init; }

    /// <summary>
    /// 在证书前的 Overlay 区间
    /// </summary>
    public PEFileRange OverlayBeforeCertificate { get; init; }

    /// <summary>
    /// 在证书后的 Overlay 区间。正常来说，应该是没有内容的。证书就是放在最后面的，不会被证书分割两个部分的内容
    /// </summary>
    public PEFileRange OverlayAfterCertificate { get; init; }
}
static class PEOverlayContentHelper
{
    /// <summary>
    /// 返回证书区间、证书前的 Overlay 和证书后的 Overlay 范围
    /// </summary>
    /// <param name="peReader"></param>
    /// <param name="fileLength"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static OverlayInfo GetOverlayAndCertificate(PEReader peReader, long fileLength)
    {
        var headers = peReader.PEHeaders;

        if (headers.PEHeader is null)
        {
            throw new ArgumentException("PEHeader is null", nameof(peReader));
        }

        // 计算最后一个节结束位置
        long lastSectionEnd = 0;
        foreach (var s in headers.SectionHeaders)
        {
            long end = (long) s.PointerToRawData + s.SizeOfRawData;
            if (end > lastSectionEnd)
            {
                lastSectionEnd = end;
            }
        }

        if (headers.SectionHeaders.Length == 0)
        {
            lastSectionEnd = headers.PEHeader.SizeOfHeaders;
        }

        // 读取 Security 目录（证书位置与大小）。注意：Security 目录的 RVA 字段是文件偏移而非 RVA。
        var certificateTableDirectory = headers.PEHeader.CertificateTableDirectory;

        // 证书 Size 是可能为 0 的，表示没有签名
        long certOffset =
            certificateTableDirectory.Size == 0 ? 0 : certificateTableDirectory.RelativeVirtualAddress; // 实际是文件偏移
        long certLength = certificateTableDirectory.Size;

        if (certOffset < 0)
        {
            // 什么情况？超过 PE 的 2GB 的签名，将读取到溢出的值
            // 此时当成没有签名处理，在安装包业务逻辑可以正确读取内容
            certOffset = 0;
            certLength = 0;
        }

        // 规范：AuthentiCode 签名位于文件靠后位置，校验覆盖证书区间之外的所有数据。
        // 我们将 Overlay 分为两段：
        // 1) 证书前的 Overlay：从 lastSectionEnd 到 certOffset（不含证书本身）。
        // 2) 证书后的 Overlay：从 certOffset+certLength 到文件末尾。

        long beforeCertOffset = 0, beforeCertLength = 0;
        long afterCertOffset = 0, afterCertLength = 0;

        if (fileLength > lastSectionEnd)
        {
            if (certLength > 0 && certOffset > lastSectionEnd)
            {
                beforeCertOffset = lastSectionEnd;
                beforeCertLength = Math.Max(0, certOffset - lastSectionEnd);
            }
            else if (certLength == 0)
            {
                // 无证书则整个 lastSectionEnd 之后都是 Overlay
                beforeCertOffset = lastSectionEnd;
                beforeCertLength = fileLength - lastSectionEnd;
            }
        }
        else
        {
            Debug.Assert(fileLength == lastSectionEnd, "文件没有包含其他内容，刚好就是 PE 末尾。不可能小于 PE 的长度");
        }

        if (certLength > 0)
        {
            long certEnd = certOffset + certLength;
            if (fileLength > certEnd)
            {
                afterCertOffset = certEnd;
                afterCertLength = fileLength - certEnd;

                // 不会存在此情况，因为证书是放在最后面的
            }
        }

        return new OverlayInfo
        {
            Certificate = new PEFileRange(certOffset, certLength),
            OverlayBeforeCertificate = new PEFileRange(beforeCertOffset, beforeCertLength),
            OverlayAfterCertificate = new PEFileRange(afterCertOffset, afterCertLength)
        };
    }
}

/// <summary>
/// 放在 PE 的 Overlay 部分的安装器内容信息
/// </summary>
public readonly record struct OverlayInstallerContentInfo() : IDisposable, IAsyncDisposable
{
    public required Stream ContentStream { get; init; }
    public required FileInfo PEFile { get; init; }

    public void Dispose()
    {
        ContentStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await ContentStream.DisposeAsync();
    }
}