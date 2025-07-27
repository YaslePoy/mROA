using System;
using System.Threading;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface ICancellationRepository
    {
        void RegisterCancellation(RequestId id, CancellationTokenSource cts);
        CancellationTokenSource? GetCancellation(RequestId id);
        void FreeCancellation(RequestId id);
    }
}