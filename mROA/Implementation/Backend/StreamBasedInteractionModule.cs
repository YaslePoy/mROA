using mROA.Abstract;

namespace mROA.Implementation.Backend;

public class StreamBasedInteractionModule : IInteractionModule
{
    private readonly Dictionary<int, Stream> _streams = new();
    private Action<int, byte[]>? _handler;

    public void RegisterSourse(Stream stream)
    {
        var id = Random.Shared.Next();
        _streams.Add(id, stream);
        _ = ListenTo((id, stream), _handler!);
    }

    public void SendTo(int clientId, byte[] message)
    {
        if (!_streams.TryGetValue(clientId, out var stream))
        {
            throw new KeyNotFoundException($"Client {clientId} not found");
        }

        stream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
        stream.Write(message, 0, message.Length);
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
            Console.WriteLine($"Client handling finished:{client.id}");
            _streams.Remove(client.id);
        }
    }

    public void Inject<T>(T dependency)
    {
        if (dependency is ISerialisationModule serialisationModule)
        {
            _handler = serialisationModule.HandleIncomingRequest;
        }
    }

}