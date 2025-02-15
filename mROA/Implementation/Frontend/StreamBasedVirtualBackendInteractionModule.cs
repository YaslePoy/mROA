using mROA.Abstract;
using mROA.Implementation.Backend;

namespace mROA.Implementation.Frontend;

public class StreamBasedVirtualBackendInteractionModule : StreamBasedInteractionModule, IInteractionModule
{
    private Stream? _serverStream { get; set; }
    private StreamBasedFrontendInteractionModule? _interactionModule;
    public void RegisterSource(Stream stream)
    {

    }
    
    public Stream GetSource(int clientId)
    {
        return _serverStream;
    }
    
    public void SendTo(int clientId, byte[] message)
    {
        _serverStream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
        _serverStream.Write(message, 0, message.Length);
    }

    private async Task ListenTo((int id, Stream stream) client, Action<int, byte[]> action)
    {
        const int bufferSize = ushort.MaxValue;
        try
        {
            byte[] buffer = new byte[bufferSize];
            while (client.stream.CanRead)
            {
                await client.stream.ReadExactlyAsync(buffer, 0, 2);
                var len = BitConverter.ToUInt16(buffer, 0);
                await client.stream.ReadExactlyAsync(buffer, 0, len);
                _ = Task.Run(() => action(client.id, buffer[..len]));
            }
        }
        catch (Exception)
        {
        }
    }

    public void Inject<T>(T dependency)
    {
        base.Inject(dependency);
        if (dependency is StreamBasedFrontendInteractionModule interactionModule)
        {
            _interactionModule = interactionModule;
        }
    }


    public void StartVirtualInteraction()
    {
        _serverStream = _interactionModule.ServerStream;
        _ = ListenTo((0, _serverStream), _handler!);
    }
}