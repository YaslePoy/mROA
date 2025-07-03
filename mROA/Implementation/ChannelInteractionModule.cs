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
        private IContextualSerializationToolKit? _serialization;
        private bool _isConnected = true;
        private bool _isActive = true;
        private TaskCompletionSource<Stream> _reconnection;
        private IEndPointContext? _context;

        public ChannelInteractionModule()
        {
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

        public IEndPointContext Context => _context;
        public Channel<NetworkMessageHeader> ReceiveChanel { get; }

        public ChannelReader<NetworkMessageHeader> TrustedPostChanel => _outputTrustedChannel.Reader;
        public ChannelReader<NetworkMessageHeader> UntrustedPostChanel => _outputUntrustedChannel.Reader;
        public Func<bool> IsConnected { get; set; } = () => false;

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
                    _context = endpointContext;
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
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId)), _context));
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
            private static Stopwatch profiler = new Stopwatch();
            private readonly Stream _ioStream;
            private readonly IContextualSerializationToolKit _serializationToolkit;
            private const int BufferSize = ushort.MaxValue;
            private readonly Memory<byte> _buffer = new byte[BufferSize];
            private bool _manualConnectionState = true;
            private readonly IEndPointContext _context;
            private readonly byte[] _lenBuffer;
            public StreamExtractor(Stream ioStream, IContextualSerializationToolKit serializationToolkit,
                IEndPointContext context)
            {
                _ioStream = ioStream;
                _serializationToolkit = serializationToolkit;
                _context = context;
                profiler.Start();
                _lenBuffer = new byte[2];
            }

            public Action<NetworkMessageHeader> MessageReceived = _ => { };

            private async Task<ushort> ReadMessageLength()
            {
                await _ioStream.ReadAsync(_lenBuffer, 0, 2);
                
                var len = BitConverter.ToUInt16(_lenBuffer);

                return len;
            }

            public async Task SingleReceive(CancellationToken token = default)
            {
                Console.WriteLine($"Receive start. Time: {profiler.ElapsedMilliseconds}ms");
                var len = await ReadMessageLength();
                Console.WriteLine($"Received len {len}. Time: {profiler.ElapsedMilliseconds}ms");
                var localSpan = _buffer[..len];

                await _ioStream.ReadExactlyAsync(localSpan, cancellationToken: token);
                Console.WriteLine($"Received. Time: {profiler.ElapsedMilliseconds}ms");
                profiler.Restart();
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
                var rawMessage = _serializationToolkit.Serialize(message, _context);
                var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));
                Console.WriteLine($"Send start. Time: {profiler.ElapsedMilliseconds}ms");

                await _ioStream.WriteAsync(header, token);
                await _ioStream.WriteAsync(rawMessage, token);
                Console.WriteLine($"Send. Time: {profiler.ElapsedMilliseconds}ms");
                profiler.Restart();
                
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