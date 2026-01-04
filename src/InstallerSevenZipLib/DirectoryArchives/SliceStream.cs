using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

/// <summary>
/// 从一个 <see cref="Stream"/> 中切出一段作为新的流来使用
/// </summary>
public class SliceStream : Stream
{
    public SliceStream(Stream originStream, long start, long length, bool leaveOpen = true)
    {
        _originStream = originStream;
        Length = length;
        _start = start;
        _leaveOpen = leaveOpen;
    }

    private readonly Stream _originStream;
    private readonly long _start;
    private readonly bool _leaveOpen;

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(Span<byte> buffer)
    {
        UpdatePositionForOriginStream();

        var toRead = (int) Math.Min(buffer.Length, Length - Position);
        buffer = buffer.Slice(0, toRead);

        var read = _originStream.Read(buffer);
        _position += read;
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        UpdatePositionForOriginStream();

        var toRead = (int) Math.Min(count, Length - Position);

        var read = await _originStream.ReadAsync(buffer, offset, toRead, cancellationToken);
        _position += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        UpdatePositionForOriginStream();

        var toRead = (int) Math.Min(buffer.Length, Length - Position);

        var read = await _originStream.ReadAsync(buffer.Slice(0, toRead), cancellationToken);

        _position += read;
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        _readCount++;

        UpdatePositionForOriginStream();

        var toRead = (int) Math.Min(count, Length - Position);

        var read = _originStream.Read(buffer, offset, toRead);
        _position += read;
        return read;
    }

    private int _readCount;

    private void UpdatePositionForOriginStream()
    {
        var position = Position + _start;
        if (_originStream.Position != position)
        {
            _originStream.Position = position;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => _position = value;
    }
    private long _position;

    protected override void Dispose(bool disposing)
    {
        if (!_leaveOpen)
        {
            _originStream.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _originStream.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
