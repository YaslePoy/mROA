using System;
using System.Threading;
using System.Threading.Tasks;
using mROA.Abstract;


namespace mROA.Implementation.Frontend
{
    public class RequestExtractor : IRequestExtractor
    {
        private readonly IExecuteModule _executeModule;

        private readonly IRepresentationModule _representationModule;
        private readonly IEndPointContext _context;

        public RequestExtractor(IExecuteModule executeModule, IRepresentationModule representationModule,
            IEndPointContext context)
        {
            _executeModule = executeModule;
            _representationModule = representationModule;
            _context = context;
        }

        public async Task StartExtraction()
        {
            var streamTokenSource = new CancellationTokenSource();

            var query = _representationModule.GetStream(Rule, _context,
                streamTokenSource.Token,
                Converters);


            await foreach (var command in query)
            {
                PushMessage(command.parced, command.originalType);
            }
        }

        public void PushMessage(object parced, EMessageType originalType)
        {
            switch (originalType)
            {
                case EMessageType.CallRequest:
                    HandleCallRequest((DefaultCallRequest)parced);
                    break;
                case EMessageType.ClientDisconnect:
                    return;
                case EMessageType.EventRequest:
                    HandleEventRequest((DefaultCallRequest)parced);
                    break;
                case EMessageType.CancelRequest:
                    HandleCancelRequest((parced as CancelRequest)!);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Predicate<NetworkMessage> Rule { get; } = m =>
            m.MessageType is EMessageType.CallRequest or EMessageType.CancelRequest
                or EMessageType.EventRequest or EMessageType.ClientDisconnect;

        public Func<NetworkMessage, Type?>[] Converters { get; } =
        {
            m => m.MessageType == EMessageType.CallRequest ? typeof(DefaultCallRequest) : null,
            m => m.MessageType == EMessageType.CancelRequest ? typeof(CancelRequest) : null,
            m => m.MessageType == EMessageType.EventRequest ? typeof(DefaultCallRequest) : null,
            m => m.MessageType == EMessageType.ClientDisconnect ? typeof(ClientDisconnect) : null
        };

        private void HandleCancelRequest(CancelRequest req)
        {
            _executeModule.Execute(req, _context.RealRepository, _representationModule, _context);
        }

        private void HandleCallRequest(DefaultCallRequest request)
        {
            var result = _executeModule.Execute(request, _context.RealRepository, _representationModule, _context);
            
            if (result is null)
            {
                return;
            }

            var resultType = result.MessageType;
            _representationModule.PostCallMessageAsync(request.Id, resultType, result, _context).ConfigureAwait(false);
        }

        private void HandleEventRequest(DefaultCallRequest request)
        {
            _executeModule.Execute(request, _context.RemoteRepository, _representationModule, _context);
        }
    }
}