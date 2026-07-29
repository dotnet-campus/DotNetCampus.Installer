namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives.Exceptions;

/// <summary>
/// 目录存档的 Header 未匹配异常，通常证明该文件不是一个有效的目录存档文件
/// </summary>
public class DirectoryArchiveHeaderNotMatchException : DirectoryArchiveException
{
    internal DirectoryArchiveHeaderNotMatchException(Stream archiveFileStream, Span<byte> actualHeader,
        ReadOnlySpan<byte> expectedHeader)
    {
        ArchiveFileStream = archiveFileStream;
        ActualHeader = actualHeader.ToArray();
        ExpectedHeader = expectedHeader.ToArray();
    }

    /// <summary>
    /// 期望的 Header 内容
    /// </summary>
    public byte[] ExpectedHeader { get; set; }

    /// <summary>
    /// 实际的 Header 内容
    /// </summary>
    public byte[] ActualHeader { get;  }

    public Stream ArchiveFileStream { get; }
}