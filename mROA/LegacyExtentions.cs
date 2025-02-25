using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace mROA
{
    public static class LegacyExtentions
    {
        public static async ValueTask<int> ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            return await stream.ReadAtLeastAsyncCore(buffer.AsMemory(offset, count), count, true, default);
        }

        public static ValueTask<int> ReadExactlyAsync(this Stream stream, Memory<byte> buffer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return stream.ReadAtLeastAsyncCore(buffer, buffer.Length, true, cancellationToken);
        }

        private static async ValueTask<int> ReadAtLeastAsyncCore(this Stream stream,
            Memory<byte> buffer,
            int minimumBytes,
            bool throwOnEndOfStream,
            CancellationToken cancellationToken)
        {
            int totalRead;
            int num;
            for (totalRead = 0; totalRead < minimumBytes; totalRead += num)
            {
                num = await stream.ReadAsync(buffer.Slice(totalRead), cancellationToken).ConfigureAwait(false);
                if (num == 0)
                {
                    if (throwOnEndOfStream)
                        throw new EndOfStreamException();
                    return totalRead;
                }
            }
            return totalRead;
        }
    }
}