using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            });
            _receiveReader = ReceiveChanel.Reader;
            _outputTrustedChannel = Channel.CreateBounded<NetworkMessageHeader>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
            });
            _trustedWriter = _outputTrustedChannel.Writer;
            _outputUntrustedChannel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
            });
            _untrustedWriter = _outputUntrustedChannel.Writer;
            _reconnection = new TaskCompletionSource<Stream>();
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

        public NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate)
        {
            return _messageBuffer.FirstOrDefault(m => predicate(m));
        }

        public event Action<int>? OnDisconnected;

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
            {
                await PostMessageAsync(
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId))));
                var ping = await ReceiveChanel.Reader.ReadAsync();
                Console.WriteLine($"Ping received {ping.Id}");
            }
            else
            {
                await _trustedWriter.WriteAsync(new NetworkMessageHeader());
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

            Console.WriteLine("Staring recovery from {0}", source);

            lock (_reconnection)
            {
                Console.WriteLine("Got lock from {0}", source);

                Console.WriteLine("Call OnDisconnected from {0}", source);
                OnDisconnected?.Invoke(ConnectionId);
            }

            Console.WriteLine("Waiting for reconnect from {0}", source);
            if (!_reconnection.Task.IsCompleted && !_isConnected)
            {
                Console.WriteLine("Current connection state {0} from {1}", _isConnected, source);
                await _reconnection.Task;
            }

            Console.WriteLine("Reconnect finished from {0}", source);
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

        public class StreamExtractor
        {
            private readonly Stream _ioStream;
            private readonly ISerializationToolkit _serializationToolkit;
            private const int BufferSize = ushort.MaxValue;
            private readonly Memory<byte> _buffer = new byte[BufferSize];
            private bool _manualConnectionState = true;

            public readonly int Id = new Random().Next();

            public StreamExtractor(Stream ioStream, ISerializationToolkit serializationToolkit)
            {
                _ioStream = ioStream;
                _serializationToolkit = serializationToolkit;
            }

            public Action<NetworkMessageHeader> MessageReceived = _ => { };

            private ushort ReadMessageLength()
            {
                var firstBit = _ioStream.ReadByte();
                if (firstBit == -1)
                {
                    _manualConnectionState = false;
                    throw new EndOfStreamException();
                }

                _manualConnectionState = true;
                var secondBit = (byte)_ioStream.ReadByte();

                var len = BitConverter.ToUInt16(new[] { (byte)firstBit, secondBit });

                return len;
            }

            public async Task SingleReceive(CancellationToken Token = default)
            {
#if TRACE
                Console.WriteLine($"[{Id}] Single receive started");
#endif
                var len = ReadMessageLength();
                var localSpan = _buffer[..len];

                await _ioStream.ReadExactlyAsync(localSpan, cancellationToken: Token);

                var message = _serializationToolkit.Deserialize<NetworkMessageHeader>(localSpan.Span);
#if TRACE
                Console.WriteLine(
                    $"{DateTime.Now.TimeOfDay} [{Id}] Received Message {message.Id} - {message.MessageType}");
                TransmissionConfig.TotalTransmittedBytes += len;
                Console.WriteLine($"Total received bytes are {TransmissionConfig.TotalTransmittedBytes}");
#endif
                MessageReceived(message);
            }

            public async Task LoopedReceive(CancellationToken token = default)
            {
#if TRACE
                Console.WriteLine("LoopedReceive started");
#endif
                while (token.IsCancellationRequested == false && IsConnected)
                {
                    await SingleReceive(token);
                }
            }

            public async Task Send(NetworkMessageHeader message, CancellationToken token = default)
            {
                try
                {
                    var rawMessage = _serializationToolkit.Serialize(message);
                    var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));

#if TRACE
                    Console.WriteLine(
                        $"{DateTime.Now.TimeOfDay} [{Id}] Posting Message {message.Id} - {message.MessageType}");
                    TransmissionConfig.TotalTransmittedBytes += rawMessage.Length;
                    Console.WriteLine($"Total received bytes are {TransmissionConfig.TotalTransmittedBytes}");
#endif

                    await _ioStream.WriteAsync(header, token);
                    await _ioStream.WriteAsync(rawMessage, token);
#if TRACE
                    Console.WriteLine(
                        $"{DateTime.Now.TimeOfDay} [{Id}] Posting finished {message.Id} - {message.MessageType}");

#endif
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public async Task SendFromChannel(ChannelReader<NetworkMessageHeader> channel,
                CancellationToken token = default)
            {
                while (token.IsCancellationRequested == false && IsConnected)
                {
                    var message = await channel.ReadAsync(token);
                    await Send(message, token);
                }
            }


            public bool IsConnected => _ioStream is { CanRead: true, CanWrite: true } && _manualConnectionState;
        }
    }
}