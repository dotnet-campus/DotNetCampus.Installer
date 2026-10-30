using DotNetCampus.Installer.Lib.Logging;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.StandardInstallerPrograms.DirectoryArchiveProviders;

/// <summary>
/// 目录归档提供器
/// </summary>
public interface IDirectoryArchiveProvider
{
    /// <summary>
    /// 将安装器内容作为目录归档读取出来
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// 约定： <br/>
    /// - 安装包里面的放入到最终安装路径的内容，应该是在 `Packing\` 相对路径下的内容 <br/>
    /// - 用完即丢的临时文件，应该是在 `Temp\` 相对路径下的内容 <br/>
    /// - 安装包本身需要依赖的运行时文件，直接放在根目录下。比如使用 Avalonia UI 的安装包，需要放置 Avalonia 相关的 DLL 文件在根目录下，如 libHarfBuzzSharp.dll 和 libSkiaSharp.dll 文件 <br/>
    /// </remarks>
    Task<IDirectoryArchive> GetDirectoryArchiveAsync(InstallerLogger logger);
}