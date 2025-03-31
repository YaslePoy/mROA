using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRepresentationModule : IInjectableModule
    {
        int Id { get; }

        Task<T> GetMessageAsync<T>(Guid? requestId = null, EMessageType? messageType = null,
            CancellationToken token = default);

        T GetMessage<T>(Guid? requestId = null, EMessageType? messageType = null);

        Task<byte[]> GetRawMessage(Guid? requestId = null, EMessageType? messageType = null,
            CancellationToken token = default);

        Task PostCallMessageAsync<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull;
        Task PostCallMessageAsync(Guid id, EMessageType eMessageType, object payload, Type payloadType);
        void PostCallMessage<T>(Guid id, EMessageType eMessageType, T payload) where T : notnull;
        void PostCallMessage(Guid id, EMessageType eMessageType, object payload, Type payloadType);
    }
}