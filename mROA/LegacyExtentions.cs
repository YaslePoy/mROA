using System.Collections.Generic;
using global::System;
using global::System.IO;
using global::System.Threading;
using global::System.Threading.Tasks;

namespace mROA
{
    public static class LegacyExtentions
    {
        public static async ValueTask ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            List < int >  x = new() { };
            await stream.ReadAtLeastAsyncCore(buffer.AsMemory(offset, count), count, true, default);
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