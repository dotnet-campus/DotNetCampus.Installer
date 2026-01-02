using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DotNetCampus.Installer.Lib.Utils.InstallerContentPackages;

/// <summary>
/// 内容包的文件块信息
/// </summary>
class ContentPackageFileBlock
{
    /// <summary>
    /// 整个 FileBlock 的长度。不包括此字段的长度
    /// </summary>
    public int FileBlockLength
    {
        get
        {
            // - FileNameLength: Int32
            // - FileName: String
            // - FileContentOffset: Int64
            // - FileLength: Int64
            int length = sizeof(int) // FileNameLength field
                         + RelativePathLength // FileName field
                         + sizeof(long) // FileContentOffset field
                         + sizeof(long); // FileLength field
            return length;
        }
    }

    /// <summary>
    /// 文件名的长度
    /// </summary>
    public int RelativePathLength { get; init; }

    /// <summary>
    /// 文件名
    /// </summary>
    public required string RelativePath
    {
        get => _relativePath;
        [MemberNotNull(nameof(_relativePath))]
        init
        {
            _relativePath = value;
            if (RelativePathLength == 0)
            {
                RelativePathLength = Encoding.UTF8.GetByteCount(_relativePath);
            }
        }
    }

    private readonly string _relativePath;

    /// <summary>
    /// 内容的偏移量
    /// </summary>
    public required long FileContentOffset { get; init; }

    /// <summary>
    /// 文件长度
    /// </summary>
    public required long FileLength { get; init; }
}