using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
        private bool _isRecovering;
        private event Action OnReconnected;

        private TaskCompletionSource<Stream> _reconection;

        public NextGenerationInteractionModule()
        {
            _reconection = new TaskCompletionSource<Stream>();
            _reconection.SetResult(Stream.Null);
        }

        public int ConnectionId { get; set; }

        public Stream? BaseStream
        {
            get => _baseStream;
            set
            {
                if (_baseStream is null)
                {
                }

                _baseStream = value;
            }
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

        public Task<NetworkMessageHeader> GetNextMessageReceiving()
        {
            if (_currentReceiving != null) return _currentReceiving;
            _currentReceiving = Task.Run(async () => await GetNextMessage());
            return _currentReceiving;
        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        private async ValueTask<bool> PostMessageInternal(NetworkMessageHeader messageHeader)
        {
            
#if TRACE
            Console.WriteLine($"{DateTime.Now.TimeOfDay} Posting message: {messageHeader.Id} - {messageHeader.MessageType} to {ConnectionId}");
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

                // Console.WriteLine("Try to get lock from post");
                // lock (_reconection)
                // {
                //     Console.WriteLine("Got lock from post");
                //     if (!_isRecovering)
                //     {
                //         _isRecovering = true;
                //         Console.WriteLine("Disconnect invoke for post");
                //         OnDisconected?.Invoke(ConnectionId);
                //         Console.WriteLine("Disconnect invoked for post");
                //     }
                // }
                //
                // withError = true;
                // Console.WriteLine("Start waiting for recovery from post");
                // _ = await _reconection.Task;
                // Console.WriteLine("Connection recovered from post");
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
                    Console.WriteLine("Recieve again");
                }

                try
                {
                    return await Receive();
                }
                catch (Exception)
                {
                    // Console.WriteLine("Try to get lock from receive");
                    // lock (_reconection)
                    // {
                    //     Console.WriteLine("Got lock from receive");
                    //
                    //     if (!_isRecovering)
                    //     {
                    //         _isRecovering = true;
                    //         Console.WriteLine("Disconnect invoke");
                    //         OnDisconected?.Invoke(ConnectionId);
                    //         Console.WriteLine("Disconnect invoked for receive");
                    //
                    //     }
                    // }
                    //
                    // withError = true;
                    // Console.WriteLine("Start waiting for recovery from receive");
                    // _ = await _reconection.Task;
                    // Console.WriteLine("Connection recovered");
                }
            }
        }

        private ushort ReadMessageLength()
        {
            var firstBit = (byte)BaseStream.ReadByte();
            var secondBit = (byte)BaseStream.ReadByte();

            var len = BitConverter.ToUInt16(new[] { firstBit, secondBit });

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
            _currentReceiving = Task.Run(async () => await GetNextMessage());

            return message;
        }

        public async Task Restart(bool sendRecovery)
        {
            if (sendRecovery)
                await PostMessageAsync(
                    new NetworkMessageHeader(_serialization!, new ClientRecovery(Math.Abs(ConnectionId))));
            _isRecovering = false;
            _reconection.SetResult(BaseStream!);
            _reconection = new TaskCompletionSource<Stream>();

        }
    }
}