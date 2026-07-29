using System;
using System.Buffers;

namespace DotNetCampus.Installer.Lib.Tests;

internal static class AssertHelper
{
    public static async Task AssertStreamEqualAsync(Stream a, Stream b)
    {
        const int length = 10240;
        var buffer1 = ArrayPool<byte>.Shared.Rent(length);
        var buffer2 = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            while (true)
            {
                var readCount1 = await a.ReadAsync(buffer1.AsMemory(0, length));
                var readCount2 = await b.ReadAsync(buffer2.AsMemory(0, length));

                Assert.AreEqual(readCount1, readCount2);

                if (readCount1 == 0)
                {
                    break;
                }

                var span1 = buffer1.AsSpan(0, readCount1);
                var span2 = buffer2.AsSpan(0, readCount2);

                Assert.IsTrue(span1.SequenceEqual(span2));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer1);
            ArrayPool<byte>.Shared.Return(buffer2);
        }
    }
}
