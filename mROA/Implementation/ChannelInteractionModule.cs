using System;
using System.Diagnostics;
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
        private readonly IContextualSerializationToolKit _serialization;
        private bool _isConnected = true;
        private bool _isActive = true;
        private TaskCompletionSource<Stream> _reconnection;

        public ChannelInteractionModule(IContextualSerializationToolKit serialization,
            IIdentityGenerator identityGenerator) : this(serialization)
        {
            ConnectionId = identityGenerator.GetNextIdentity();
        }

        public ChannelInteractionModule(IContextualSerializationToolKit serialization)
        {
            _serialization = serialization;
            ReceiveChanel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
            });
            _receiveReader = ReceiveChanel.Reader;
            _outputTrustedChannel = Channel.CreateUnbounded<NetworkMessageHeader>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
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

        public IEndPointContext Context { get; set; }
        public Channel<NetworkMessageHeader> ReceiveChanel { get; }

        public ChannelReader<NetworkMessageHeader> TrustedPostChanel => _outputTrustedChannel.Reader;
        public ChannelReader<NetworkMessageHeader> UntrustedPostChanel => _outputUntrustedChannel.Reader;
        public Func<bool> IsConnected { get; set; } = () => false;

        public ValueTask<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true)
        {
            return _receiveReader.ReadAsync();
        }
        private async ValueTask<bool> PostMessageInternal(NetworkMessageHeader messageHeader)
        {
            if (!IsConnected())
            {
                return false;
            }

            await _trustedWriter.WriteAsync(messageHeader);
            return true;
        }

        public async Task PostMessageAsync(NetworkMessageHeader messageHeader)
        {
            while (true)
            {
                if (await PostMessageInternal(messageHeader))
                    break;

                if (!_isActive)
                {
                    return;
                }

                _isConnected = false;
                await MakeRecovery();
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
                    new NetworkMessageHeader(_serialization, new ClientRecovery(Math.Abs(ConnectionId)), Context));
                await ReceiveChanel.Reader.ReadAsync();
            }
            else
            {
                await _trustedWriter.WriteAsync(new NetworkMessageHeader());
            }

            PassReconnection();
        }

        public void PassReconnection()
        {
            _reconnection.TrySetResult(Stream.Null);
            _isConnected = true;
            _reconnection = new TaskCompletionSource<Stream>();
        }

        private async Task MakeRecovery()
        {
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
        }

        public class StreamExtractor
        {
            private const int BufferSize = ushort.MaxValue;

            private readonly Stream _ioStream;
            private readonly IContextualSerializationToolKit _serializationToolkit;
            private readonly Memory<byte> _buffer = new byte[BufferSize];
            private readonly IEndPointContext _context;
            private readonly byte[] _lenBuffer;

            public StreamExtractor(Stream ioStream, IContextualSerializationToolKit serializationToolkit,
                IEndPointContext context)
            {
                _ioStream = ioStream;
                _serializationToolkit = serializationToolkit;
                _context = context;
                _lenBuffer = new byte[2];
            }

            public Action<NetworkMessageHeader> MessageReceived = _ => { };

            private async Task<ushort> ReadMessageLength()
            {
                await _ioStream.ReadAsync(_lenBuffer);

                var len = BitConverter.ToUInt16(_lenBuffer);
            
                return len;
            }

            public async Task SingleReceive(CancellationToken token = default)
            {
                var len = await ReadMessageLength();
                var localSpan = _buffer[..len];

                await _ioStream.ReadExactlyAsync(localSpan, cancellationToken: token);
                var message = _serializationToolkit.Deserialize<NetworkMessageHeader>(localSpan, _context);
                MessageReceived(message);
            }

            public async Task LoopedReceive(CancellationToken token = default)
            {
                while (token.IsCancellationRequested == false && IsConnected)
                {
                    await SingleReceive(token);
                }
            }

            private async Task Send(NetworkMessageHeader message, CancellationToken token = default)
            {
                var bodySpan = _buffer[2..];
                var len = _serializationToolkit.Serialize(message, bodySpan.Span, _context);
                var header = BitConverter.GetBytes((ushort)len);
                header.CopyTo(_buffer);
                var sendingSpan = _buffer[..(len + 2)];
                await _ioStream.WriteAsync(sendingSpan, token);
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

            public bool IsConnected => _ioStream is { CanRead: true, CanWrite: true };
        }
    }
}