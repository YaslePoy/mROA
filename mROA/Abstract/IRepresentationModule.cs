using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRepresentationModule : IInjectableModule
    {
        int Id { get; }

        Task<(object? Deserialized, EMessageType MessageType)> GetSingle(Predicate<NetworkMessageHeader> rule,
            CancellationToken token,
            params Func<NetworkMessageHeader, Type?>[] converter);
        
        IAsyncEnumerable<(object parced, EMessageType originalType)> GetStream(Predicate<NetworkMessageHeader> rule, CancellationToken token,
            params Func<NetworkMessageHeader, Type?>[] converter);

        Task PostCallMessageAsync<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull;
        void PostCallMessage<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull;
        void PostCallMessage(Guid id, EMessageType eMessageType, object payload, Type payloadType);
    }
}