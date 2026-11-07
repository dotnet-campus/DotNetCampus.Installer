using System.Buffers;

namespace DotNetCampus.Installer.Lib.Utils.InstallerArrayPools;

internal static class InstallerArrayPool
{
    public static InstallerPoolArrayContext<T> Rent<T>(int minimumLength)
    {
        ArrayPool<T> arrayPool = ArrayPool<T>.Shared;
        T[] buffer = arrayPool.Rent(minimumLength);
        var textPoolArrayContext = new InstallerPoolArrayContext<T>(buffer, 0, minimumLength, arrayPool);
        return textPoolArrayContext;
    }
}