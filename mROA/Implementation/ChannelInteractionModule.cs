using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ChannelInteractionModule : IChannelInteractionModule
    {
        private readonly ChannelReader<NetworkMessageHeader> _receiveReader;
        private readonly ChannelWriter<NetworkMessageHeader> _trustedWriter;
        private readonly ChannelWriter<NetworkMessageHeader> _untrustedWriter;
        private readonly Channel<NetworkMessageHeader> _outputTrustedChannel;
        private readonly Channel<NetworkMessageHeader> _outputUntrustedChannel;
        private readonly List<NetworkMessageHeader> _messageBuffer = new(128);
        private Task<NetworkMessageHeader>? _currentReceiving;
        private ISerializationToolkit? _serialization;
        private bool _isConnected = true;
        private bool _isActive = true;
        private TaskCompletionSource<Stream> _reconnection;

        public ChannelInteractionModule()
        {
            ReceiveChanel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = true
            });
            _receiveReader = ReceiveChanel.Reader;
            _outputTrustedChannel = Channel.CreateBounded<NetworkMessageHeader>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
                
            });
            _trustedWriter = _outputTrustedChannel.Writer;
            _outputUntrustedChannel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true
            });
            _untrustedWriter = _outputUntrustedChannel.Writer;
        }

        public int ConnectionId { get; set; }

        public Channel<NetworkMessageHeader> ReceiveChanel { get; }

        public ChannelReader<NetworkMessageHeader> TrustedPostChanel => _outputTrustedChannel.Reader;
        public ChannelReader<NetworkMessageHeader> UntrustedPostChanel => _outputUntrustedChannel.Reader;
        public Func<bool> IsConnected { get; set; }


        public void Inject<T>(T dependency)
        {
            switch (dependency)
            {
                case ISerializationToolkit toolkit:
                    _serialization = toolkit;
                    break;
                case IIdentityGenerator identityGenerator:
                    ConnectionId = identityGenerator.GetNextIdentity();
                    break;
            }
        }

        public ValueTask<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true)
        {
            return _receiveReader.ReadAsync();
            // if (_currentReceiving != null) return _currentReceiving;
            // _currentReceiving = Task.Run(async () => await GetNextMessage());
            // return _currentReceiving;
        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        private async ValueTask<bool> PostMessageInternal(NetworkMessageHeader messageHeader)
        {
            if (!IsConnected())
            {
                return false;
            }
            
            await _trustedWriter.WriteAsync(messageHeader);
            return true;
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.


        public async Task PostMessageAsync(NetworkMessageHeader messageHeader)
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            // Console.WriteLine("Sending {0}", JsonSerializer.Serialize(message));

            bool withError = false;
            while (true)
            {
                if (withError)
                {
                    Console.WriteLine("Post again");
                }

                if (await PostMessageInternal(messageHeader))
                    break;

                if (!_isActive)
                {
                    return;
                }

                _isConnected = false;
                withError = true;
                await MakeRecovery("OUT");
            }
        }

        public async Task PostMessageUntrustedAsync(NetworkMessageHeader messageHeader)
        {
            await _untrustedWriter.WriteAsync(messageHeader);
        }

        public void HandleMessage(NetworkMessageHeader messageHeader)
        {
            _messageBuffer.Remove(messageHeader);
        }

        // public NetworkMessageHeader[] UnhandledMessages => _messageBuffer.ToArray();

        public NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate)
        {
            return _messageBuffer.FirstOrDefault(m => predicate(m));
        }

        public event Action<int>? OnDisconnected;

        private async Task<NetworkMessageHeader> GetNextMessage()
        {
            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is null");

            bool withError = false;

            while (true)
            {
                if (withError)
                {
                    Console.WriteLine("Receive again");
                }

                try
                {
                    var message = await _receiveReader.ReadAsync();
                    return message;
                }
                catch (Exception)
                {
                    if (!_isActive)
                    {
                        return NetworkMessageHeader.Null;
                    }

                    withError = true;
                    await MakeRecovery("IN");
                }
            }
        }

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
            {
                await PostMessageAsync(
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId))));
            }

            Console.WriteLine("Setting result for reconnection");
            var setting = _reconnection.TrySetResult(null);
            // _isInReconnectionState = false;
            _isConnected = true;
            Console.WriteLine($"Set result for reconnection {setting}");

            _reconnection = new TaskCompletionSource<Stream>();
        }

        private async Task MakeRecovery(string source)
        {
            //TODO переделать реконнект
            
            // Console.WriteLine("Staring recovery from {0}", source);
            //
            // lock (_reconnection)
            // {
            //     Console.WriteLine("Got lock from {0}", source);
            //
            //     Console.WriteLine("Call OnDisconnected from {0}", source);
            //     _isInReconnectionState = true;
            //     OnDisconnected?.Invoke(ConnectionId);
            // }
            //
            // Console.WriteLine("Waiting for reconnect from {0}", source);
            // if (!_reconnection.Task.IsCompleted && !_isConnected)
            // {
            //     Console.WriteLine("Current connection state {0} from {1}", _isConnected, source);
            //     await _reconnection.Task;
            // }
            //
            // Console.WriteLine("Reconnect finished from {0}", source);
            // lock (_reconnection)
            // {
            //     _isInReconnectionState = false;
            // }
        }

        public void Dispose()
        {
            Console.WriteLine("Interaction module disposed");
            _isActive = false;
            if (_currentReceiving is { IsCompleted: true })
            {
                _currentReceiving?.Dispose();
            }
        }
    }
}