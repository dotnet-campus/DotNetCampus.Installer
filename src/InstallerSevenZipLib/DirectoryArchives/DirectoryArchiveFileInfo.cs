using Microsoft.DotNet.Archive;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public readonly record struct DirectoryArchiveFileInfo(string RelativePath, FileInfo FileInfo)
{
    /// <summary>
    /// 压缩模式，允许带无压缩模式，方便提升性能
    /// </summary>
    public CompressMode CompressMode { get; init;  } = CompressMode.LZMA;
}

/// <summary>
/// 压缩模式
/// </summary>
public enum CompressMode : byte
{
    /// <summary>
    /// 无压缩
    /// </summary>
    NoCompression,

    /// <summary>
    /// 使用 LZMA 压缩
    /// </summary>
    LZMA,
}