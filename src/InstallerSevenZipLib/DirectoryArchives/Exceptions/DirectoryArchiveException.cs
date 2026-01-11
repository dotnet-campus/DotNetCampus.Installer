using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives.Exceptions;

/// <summary>
/// 目录存档异常的基类
/// </summary>
public abstract class DirectoryArchiveException : Exception
{
    protected DirectoryArchiveException()
    {
    }

    protected DirectoryArchiveException(string? message) : base(message)
    {
    }

    protected DirectoryArchiveException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}