using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CancellationRepository : ICancellationRepository
    {
        private readonly ConcurrentDictionary<RequestId, CancellationTokenSource> _cancellations = new();

        public void RegisterCancellation(RequestId id, CancellationTokenSource cts)
        {
            _cancellations.TryAdd(id, cts);
        }

        public CancellationTokenSource? GetCancellation(RequestId id)
        {
            return _cancellations.GetValueOrDefault(id);
        }

        public void FreeCancellation(RequestId id)
        {
            _cancellations.Remove(id, out _);
        }
    }
}