using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRepresentationModule
    {
        int Id { get; }
        IEndPointContext Context { get; }

        Task<(object? Deserialized, EMessageType MessageType)> GetSingle(Predicate<NetworkMessage> rule,
            IEndPointContext? context, CancellationToken token = default,
            params Func<NetworkMessage, Type?>[] converter);

        IAsyncEnumerable<(object parced, EMessageType originalType)> GetStream(Predicate<NetworkMessage> rule,
            IEndPointContext? context, CancellationToken token = default,
            params Func<NetworkMessage, Type?>[] converter);

        Task PostCallMessageAsync<T>(RequestId id, EMessageType eMessageType, T payload, IEndPointContext? context)
            where T : notnull;

        void PostCallMessage<T>(RequestId id, EMessageType eMessageType, T payload, IEndPointContext? context)
            where T : notnull;

        Task PostCallMessageUntrustedAsync<T>(RequestId id, EMessageType eMessageType, T payload,
            IEndPointContext? context)
            where T : notnull;
    }
}