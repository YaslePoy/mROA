using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRepresentationModule : IInjectableModule
    {
        int Id { get; }

        Task<T> GetMessageAsync<T>(Guid? requestId = null, MessageType? messageType = null,
            CancellationToken token = default);

        T GetMessage<T>(Guid? requestId = null, MessageType? messageType = null);

        Task<byte[]> GetRawMessage(Guid? requestId = null, MessageType? messageType = null,
            CancellationToken token = default);

        Task PostCallMessageAsync<T>(Guid id, MessageType messageType, T payload) where T : notnull;
        Task PostCallMessageAsync(Guid id, MessageType messageType, object payload, Type payloadType);
        void PostCallMessage<T>(Guid id, MessageType messageType, T payload) where T : notnull;
        void PostCallMessage(Guid id, MessageType messageType, object payload, Type payloadType);
    }
}