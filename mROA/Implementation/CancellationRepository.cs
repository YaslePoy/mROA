using System;
using System.Collections.Generic;
using System.Threading;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CancellationRepository : ICancellationRepository
    {
        private Dictionary<Guid, CancellationTokenSource> _cancellations = new(); 
        public void RegisterCancellation(Guid id, CancellationTokenSource cts)
        {
            _cancellations.TryAdd(id, cts);
        }

        public CancellationTokenSource? GetCancellation(Guid id)
        {
            return _cancellations.GetValueOrDefault(id, null);
        }

        public void FreeCancelation(Guid id)
        {
            _cancellations.Remove(id);
        }

        public void Inject<T>(T dependency)
        {
            
        }
    }
}