using System.Buffers;

namespace DotNetCampus.Installer.Lib.Utils.InstallerArrayPools;

/// <summary>
/// 数组池里面的数组信息。使用时记得调用释放哦
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct InstallerPoolArrayContext<T> : IDisposable
{
    /// <summary>
    /// 租用一个指定最小长度的数组
    /// </summary>
    /// <param name="minimumLength"></param>
    /// <returns></returns>
    public static InstallerPoolArrayContext<T> Rent(int minimumLength)
    {
        return InstallerArrayPool.Rent<T>(minimumLength);
    }

    internal InstallerPoolArrayContext(T[] buffer, int start, int length, ArrayPool<T> arrayPool)
    {
        _buffer = buffer;
        _start = start;
        _length = length;
        _arrayPool = arrayPool;
    }

    private readonly T[] _buffer;

    private readonly int _start;
    private readonly int _length;

    private readonly ArrayPool<T> _arrayPool;

    /// <summary>
    /// 获取 Span 类型的内容
    /// </summary>
    public Span<T> Span => _buffer.AsSpan(_start, _length);

    /// <summary>
    /// 获取 Memory 类型的内容
    /// </summary>
    public Memory<T> Memory => _buffer.AsMemory(_start, _length);

    /// <summary>
    /// 分割
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public InstallerPoolArrayContext<T> Slice(int start)
    {
        return Slice(start, _length - start);
    }

    /// <summary>
    /// 分割
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public InstallerPoolArrayContext<T> Slice(int start, int count)
    {
        return new InstallerPoolArrayContext<T>(_buffer, _start + start, count, _arrayPool);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}