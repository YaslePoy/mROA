using System;
using System.Threading;

namespace mROA.Abstract
{
    public interface ICancellationRepository : IInjectableModule
    {
        void RegisterCancellation(Guid id, CancellationTokenSource cts);
        CancellationTokenSource? GetCancellation(Guid id);
        void FreeCancelation(Guid id);
    }
}