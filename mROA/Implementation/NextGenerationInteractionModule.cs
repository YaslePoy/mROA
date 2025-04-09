using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class NextGenerationInteractionModule : INextGenerationInteractionModule
    {
        private int DebugId = new Random().Next();
        private const int BufferSize = ushort.MaxValue;
        private readonly Memory<byte> _buffer = new byte[BufferSize];
        private readonly List<NetworkMessageHeader> _messageBuffer = new(128);
        private Task<NetworkMessageHeader>? _currentReceiving;
        private ISerializationToolkit? _serialization;
        private Stream? _baseStream;
        private bool _isConnected = true;
        private bool _isInReconnectionState;
        private bool _isActive = true;
        private TaskCompletionSource<Stream> _reconnection;

        public NextGenerationInteractionModule()
        {
            _reconnection = new TaskCompletionSource<Stream>();
        }

        public int ConnectionId { get; set; }

        public Stream? BaseStream
        {
            get => _baseStream; set => _baseStream = value;
        }


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

        public Task<NetworkMessageHeader> GetNextMessageReceiving(bool infinite = true)
        {
            if (!infinite) return Receive().AsTask();
            if (_currentReceiving != null) return _currentReceiving;
            _currentReceiving = Task.Run(async () => await GetNextMessage());
            return _currentReceiving;

        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        private async ValueTask<bool> PostMessageInternal(NetworkMessageHeader messageHeader)
        {
#if TRACE
            Console.WriteLine(
                $"{DateTime.Now.TimeOfDay} Posting message: {messageHeader.Id} - {messageHeader.MessageType} to {ConnectionId}");
#endif

            var rawMessage = _serialization.Serialize(messageHeader);
            var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));

            if (!_baseStream.CanWrite)
                return false;
    
            await BaseStream.WriteAsync(header);
            await BaseStream.WriteAsync(rawMessage);
            return true;
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.


        public async Task PostMessageAsync(NetworkMessageHeader messageHeader)
        {
            if (BaseStream == null)
                throw new NullReferenceException("BaseStream is null");

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

        public void HandleMessage(NetworkMessageHeader messageHeader)
        {
            _messageBuffer.Remove(messageHeader);
        }

        public NetworkMessageHeader[] UnhandledMessages => _messageBuffer.ToArray();

        public NetworkMessageHeader? FirstByFilter(Predicate<NetworkMessageHeader> predicate)
        {
            return _messageBuffer.FirstOrDefault(m => predicate(m));
        }

        public event Action<int>? OnDisconected;

        private async Task<NetworkMessageHeader> GetNextMessage()
        {
            if (BaseStream == null)
                throw new NullReferenceException("BaseStream is null");

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
                    var message = await Receive();
                    _currentReceiving = Task.Run(async () => await GetNextMessage());

                    return message;
                }
                catch (Exception ex)
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

        private ushort ReadMessageLength()
        {
            var firstBit = BaseStream.ReadByte();
            if (firstBit == -1)
            {
                _isConnected = false;
                throw new EndOfStreamException();
            }

            _isConnected = true;
            var secondBit = (byte)BaseStream.ReadByte();

            var len = BitConverter.ToUInt16(new[] { (byte)firstBit, secondBit });

            return len;
        }

        private async ValueTask<NetworkMessageHeader> Receive()
        {
            var len = ReadMessageLength();
            var localSpan = _buffer[..len];

            await BaseStream.ReadExactlyAsync(localSpan);

            var message = _serialization.Deserialize<NetworkMessageHeader>(localSpan.Span);
#if TRACE
            Console.WriteLine($"{DateTime.Now.TimeOfDay} Received Message {message.Id} - {message.MessageType}");
            TransmissionConfig.TotalTransmittedBytes += len;
            Console.WriteLine($"Total received bytes are {TransmissionConfig.TotalTransmittedBytes}");
#endif
            _messageBuffer.Add(message);
            return message;
        }

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
            {
                await PostMessageAsync(
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId))));
                var iTest = _baseStream.ReadByte();
                var bTest = (byte)iTest;
                _baseStream.WriteByte(bTest);
            }
            else
            {
                const byte confirmByte = 128;
                _baseStream.WriteByte(confirmByte);
                var iPong = _baseStream.ReadByte();
                var bPong = (byte)iPong;
                if (confirmByte != bPong)
                {
                    Console.WriteLine("Incorrect byte");
                }
            }

            Console.WriteLine("Setting result for reconnection");
            var setting = _reconnection.TrySetResult(BaseStream);
            _isInReconnectionState = false;
            _isConnected = true;
            Console.WriteLine($"Set result for reconnection {setting}");

            _reconnection = new TaskCompletionSource<Stream>();
        }

        private async Task MakeRecovery(string source)
        {
            Console.WriteLine("Staring recovery from {0}", source);

            lock (_reconnection)
            {
                Console.WriteLine("Got lock from {0}", source);
                if (_isConnected || _isInReconnectionState)
                {
                    Console.WriteLine(
                        $"{source} {_isConnected} {_isInReconnectionState} {!_baseStream.CanRead} {!_baseStream.CanWrite}");
                    return;
                }

                Console.WriteLine("Call OnDisconnected from {0}", source);
                _isInReconnectionState = true;
                OnDisconected?.Invoke(ConnectionId);
            }

            Console.WriteLine("Waiting for reconnect from {0}", source);
            if (!_reconnection.Task.IsCompleted && !_isConnected)
            {
                Console.WriteLine("Current connection state {0} from {1}", _isConnected, source);
                await _reconnection.Task;
            }

            Console.WriteLine("Reconnect finished from {0}", source);
            lock (_reconnection)
            {
                _isInReconnectionState = false;
            }
        }

        public void Dispose()
        {
            _isActive = false;
            _currentReceiving?.Dispose();
            _baseStream?.Dispose();
        }
    }
}