using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CancellationRepository : ICancellationRepository
    {
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();

        public void RegisterCancellation(Guid id, CancellationTokenSource cts)
        {
            _cancellations.TryAdd(id, cts);
        }

        public CancellationTokenSource? GetCancellation(Guid id)
        {
            return _cancellations.GetValueOrDefault(id);
        }

        public void FreeCancelation(Guid id)
        {
            _cancellations.Remove(id, out _);
        }

        public void Inject(object dependency)
        {
        }
    }
}