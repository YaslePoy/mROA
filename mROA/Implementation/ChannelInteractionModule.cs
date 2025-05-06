using System;
using System.IO;
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
        private Task<NetworkMessageHeader>? _currentReceiving;
        private IContextualSerializationToolKit? _serialization;
        private bool _isConnected = true;
        private bool _isActive = true;
        private TaskCompletionSource<Stream> _reconnection;
        public IEndPointContext Context { get; set; }

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
                case IContextualSerializationToolKit toolkit:
                    _serialization = toolkit;
                    break;
                case IIdentityGenerator identityGenerator:
                    ConnectionId = identityGenerator.GetNextIdentity();
                    break;
                case IEndPointContext endpointContext:
                    Context = endpointContext;
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
            
            bool withError = false;
            while (true)
            {
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

        public event Action<int>? OnDisconnected;

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
            {
                await PostMessageAsync(
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId)), Context));
                var ping = await ReceiveChanel.Reader.ReadAsync();
            }
            else
            {
                await _trustedWriter.WriteAsync(new NetworkMessageHeader());
            }

            var setting = _reconnection.TrySetResult(null);
            // _isInReconnectionState = false;
            _isConnected = true;
            _reconnection = new TaskCompletionSource<Stream>();
        }

        private async Task MakeRecovery(string source)
        {
            //TODO переделать реконнект
            lock (_reconnection)
            {
                OnDisconnected?.Invoke(ConnectionId);
            }

            if (!_reconnection.Task.IsCompleted && !_isConnected)
            {
                await _reconnection.Task;
            }
        }

        public void Dispose()
        {
            _isActive = false;
            if (_currentReceiving is { IsCompleted: true })
            {
                _currentReceiving?.Dispose();
            }
        }

        public class StreamExtractor
        {
            private readonly Stream _ioStream;
            private readonly IContextualSerializationToolKit _serializationToolkit;
            private const int BufferSize = ushort.MaxValue;
            private readonly Memory<byte> _buffer = new byte[BufferSize];
            private bool _manualConnectionState = true;
            private IEndPointContext Context;
            public readonly int Id = new Random().Next();

            public StreamExtractor(Stream ioStream, IContextualSerializationToolKit serializationToolkit,
                IEndPointContext context)
            {
                _ioStream = ioStream;
                _serializationToolkit = serializationToolkit;
                Context = context;
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
                var len = ReadMessageLength();
                var localSpan = _buffer[..len];

                await _ioStream.ReadExactlyAsync(localSpan, cancellationToken: Token);

                var message = _serializationToolkit.Deserialize<NetworkMessageHeader>(localSpan, Context);
                MessageReceived(message);
            }

            public async Task LoopedReceive(CancellationToken token = default)
            {
                while (token.IsCancellationRequested == false && IsConnected)
                {
                    await SingleReceive(token);
                }
            }

            public async Task Send(NetworkMessageHeader message, CancellationToken token = default)
            {
                var rawMessage = _serializationToolkit.Serialize(message, Context);
                var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));

                await _ioStream.WriteAsync(header, token);
                await _ioStream.WriteAsync(rawMessage, token);
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