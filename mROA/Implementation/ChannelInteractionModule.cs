using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ChannelInteractionModule : IChannelInteractionModule
    {
        private readonly ChannelReader<NetworkMessage> _receiveReader;
        private readonly ChannelWriter<NetworkMessage> _trustedWriter;
        private readonly ChannelWriter<NetworkMessage> _untrustedWriter;
        private readonly Channel<NetworkMessage> _outputTrustedChannel;
        private readonly Channel<NetworkMessage> _outputUntrustedChannel;
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
            ReceiveChanel = Channel.CreateUnbounded<NetworkMessage>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
            });
            _receiveReader = ReceiveChanel.Reader;
            _outputTrustedChannel = Channel.CreateUnbounded<NetworkMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
            _trustedWriter = _outputTrustedChannel.Writer;
            _outputUntrustedChannel = Channel.CreateUnbounded<NetworkMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
            });
            _untrustedWriter = _outputUntrustedChannel.Writer;
            _reconnection = new TaskCompletionSource<Stream>();
        }

        public int ConnectionId { get; set; }

        public IEndPointContext Context { get; set; }
        public Channel<NetworkMessage> ReceiveChanel { get; }

        public ChannelReader<NetworkMessage> TrustedPostChanel => _outputTrustedChannel.Reader;
        public ChannelReader<NetworkMessage> UntrustedPostChanel => _outputUntrustedChannel.Reader;
        public Func<bool> IsConnected { get; set; } = () => false;

        public ValueTask<NetworkMessage> GetNextMessageReceiving()
        {
            return _receiveReader.ReadAsync();
        }

        private async ValueTask<bool> PostMessageInternal(NetworkMessage message)
        {
            if (!IsConnected())
            {
                return false;
            }

            await _trustedWriter.WriteAsync(message);
            return true;
        }

        public async Task PostMessageAsync(NetworkMessage message)
        {
            while (true)
            {
                if (await PostMessageInternal(message))
                    break;

                if (!_isActive)
                {
                    return;
                }

                _isConnected = false;
                await MakeRecovery();
            }
        }

        public async Task PostMessageUntrustedAsync(NetworkMessage message)
        {
            await _untrustedWriter.WriteAsync(message);
        }

        public event Action<int>? OnDisconnected;

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
            {
                await PostMessageAsync(
                    new NetworkMessage(_serialization, new ClientRecovery(Math.Abs(ConnectionId)), Context));
                await ReceiveChanel.Reader.ReadAsync();
            }
            else
            {
                await _trustedWriter.WriteAsync(new NetworkMessage());
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
            private const int BufferSize = ushort.MaxValue + 19;

            private readonly Stream _ioStream;
            private readonly Memory<byte> _buffer = new byte[BufferSize];

            public StreamExtractor(Stream ioStream)
            {
                _ioStream = ioStream;
            }

            public Action<NetworkMessage> MessageReceived = _ => { };
            
            public async Task SingleReceive(CancellationToken token = default)
            {
                var firstRead = await _ioStream.ReadAsync(_buffer, token);
                
                var meta = MemoryMarshal.Read<NetworkMessage.NetworkMessageMeta>(_buffer.Span);
                
                var len = meta.BodyLength;
                var readLen = firstRead - 19;
                
                if (readLen != len)
                {
                    var lastPart = _buffer[firstRead..(len + 19)];
                    await _ioStream.ReadExactlyAsync(lastPart, cancellationToken: token);
                }

                var message = meta.ToMessage(_buffer.Span);
#if TRACE
                Console.WriteLine("RECV " + message);
#endif
                MessageReceived(message);
            }

            public async Task LoopedReceive(CancellationToken token = default)
            {
                while (token.IsCancellationRequested == false && IsConnected)
                {
                    await SingleReceive(token);
                }
            }

            private async Task Send(NetworkMessage message, CancellationToken token = default)
            {
                var meta = message.ToMeta();
                MemoryMarshal.Write(_buffer.Span, ref meta);
                message.Data.CopyTo(_buffer.Span[19..]);
                var sendingSpan = _buffer[..(19 + meta.BodyLength)];
                await _ioStream.WriteAsync(sendingSpan, token);
                #if TRACE
                Console.WriteLine("SEND " + message);
                #endif
                // _logger.LogTrace("SEND {0}", message.ToString());
            }

            public async Task SendFromChannel(ChannelReader<NetworkMessage> channel,
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