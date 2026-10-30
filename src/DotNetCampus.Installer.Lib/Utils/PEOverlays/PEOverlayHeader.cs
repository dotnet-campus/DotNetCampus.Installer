using System;
using System.Net;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DotNetCampus.Installer.Lib.Utils.PEOverlays;

/// <summary>
/// 表示存放在 PE 文件 Overlay 部分的安装器内容的头部标识
/// </summary>
/// PE 文件的 Overlay 指的是“超出 PE 结构本身的尾随数据”，也就是在最后一个节（Section）的 PointerToRawData + SizeOfRawData 之后、直到文件末尾的那段附加内容。它不属于标准的 PE 头或节表描述的范围，通常由打包器、安装器、自解压程序、某些保护/加壳工具或自定义嵌入逻辑写入，用于携带额外的资源、配置或有效载荷。
/// 常见的别称包括：
/// •	Overlay（最常用）
/// •	Appended data / Trailing data（追加/尾随数据）
/// •	Extra data / EOF overlay（文件末尾的额外数据）
/// •	Payload（有效载荷）
/// •	Data beyond image / Data after PE（超出镜像范围的数据） -（在某些工具/社区语境中）Stub data（被误称为“存根数据”，但更准确地说，DOS Stub 是 PE 标准的一部分，非 Overlay）
internal static class PEOverlayHeader
{
    /// <summary>
    /// 放在 PE 文件 Overlay 部分的安装器内容的头部标识。刚好是 64 字节长度
    /// </summary>
    /// COMMIT: efdbcf93c7c3c2a114e7445af0191225627593f2
    /// 内容： 43 6F 6E 74 65 6E 74 20 6F 66 20 49 6E 73 74 61 6C 6C 65 72 20 69 6E 20 50 45 20 66 69 6C 65 20 4F 76 65 72 6C 61 79 20 70 61 79 6C 6F 61 64 20 31 2E 30 3B 20 54 68 65 20 4C 65 6E 67 74 68 3A
    public static ReadOnlySpan<byte> ContentOfInstallerOverlayHeader => "Content of Installer in PE file Overlay payload 1.0; The Length:"u8;
}