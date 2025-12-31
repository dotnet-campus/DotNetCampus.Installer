using System;

namespace DotNetCampus.Installer.Lib.Utils.PEOverlays;

internal static class PEOverlayHeader
{
    /// <summary>
    /// 放在 PE 文件 Overlay 部分的安装器内容的头部标识。刚好是 64 字节长度
    /// </summary>
    /// COMMIT: efdbcf93c7c3c2a114e7445af0191225627593f2
    /// 内容： 43 6F 6E 74 65 6E 74 20 6F 66 20 49 6E 73 74 61 6C 6C 65 72 20 69 6E 20 50 45 20 66 69 6C 65 20 4F 76 65 72 6C 61 79 20 70 61 79 6C 6F 61 64 20 31 2E 30 3B 20 54 68 65 20 4C 65 6E 67 74 68 3A
    public static ReadOnlySpan<byte> ContentOfInstallerOverlayHeader => "Content of Installer in PE file Overlay payload 1.0; The Length:"u8;
}