using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation.Frontend;

public class RequestExtractor : IRequestExtractor
{
    private IRepresentationModule? _representationModule;
    private IContextRepository? _contextRepository;
    private IMethodRepository? _methodRepository;
    private IExecuteModule? _executeModule;
    private ISerializationToolkit? _serializationToolkit;

    public void Inject<T>(T dependency)
    {
        switch (dependency)
        {
            case IExecuteModule executeModule:
                _executeModule = executeModule;
                break;
            case IContextRepository contextRepository:
                _contextRepository = contextRepository;
                break;
            case IMethodRepository methodRepository:
                _methodRepository = methodRepository;
                break;
            case IRepresentationModule representationModule:
                _representationModule = representationModule;
                break;
            case ISerializationToolkit serializationToolkit:
                _serializationToolkit = serializationToolkit;
                break;
        }
    }

    public async Task StartExtraction()
    {
        if (_serializationToolkit == null)
            throw new NullReferenceException("Serializing toolkit is null.");
        if (_executeModule == null)
            throw new NullReferenceException("Execute module is null.");
        if (_contextRepository == null)
            throw new NullReferenceException("Context repository is null.");
        if (_representationModule == null)
            throw new NullReferenceException("Representation module is null.");
        if (_methodRepository == null)
            throw new NullReferenceException("Method repository is null.");

        var multiClientOwnershipRepository = TransmissionConfig.OwnershipRepository as MultiClientOwnershipRepository;


        multiClientOwnershipRepository?.RegisterOwnership(_representationModule.Id);

        try
        {
            while (true)
            {
                var request =
                    await _representationModule!.GetMessage<DefaultCallRequest>(messageType: MessageType.CallRequest);

                if (request.Parameter is not null)
                {
                    var parameterType = _methodRepository!.GetMethod(request.CommandId).GetParameters().First()
                        .ParameterType;

                    request.Parameter = _serializationToolkit.Cast(request.Parameter, parameterType);
                }

                var result = _executeModule.Execute(request, _contextRepository);

                var resultType = result is FinalCommandExecution
                    ? MessageType.FinishedCommandExecution
                    : MessageType.ExceptionCommandExecution;

                await _representationModule.PostCallMessage(request.CallRequestId, resultType, result);
            }
        }
        catch
        {
            multiClientOwnershipRepository?.RegisterOwnership(_representationModule.Id);
        }
    }
}