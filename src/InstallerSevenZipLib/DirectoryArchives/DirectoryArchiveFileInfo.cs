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
}