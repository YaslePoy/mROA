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
        private const int BufferSize = ushort.MaxValue;
        private readonly Memory<byte> _buffer = new byte[BufferSize];
        private readonly List<NetworkMessage> _messageBuffer = new(128);
        private Task<NetworkMessage>? _currentReceiving;
        private ISerializationToolkit? _serialization;
        public int ConnectionId { get; set; }
        public Stream? BaseStream { get; set; }


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

        public Task<NetworkMessage> GetNextMessageReceiving()
        {
            if (_currentReceiving != null) return _currentReceiving;
            _currentReceiving = Task.Run(async () => await GetNextMessage());
            return _currentReceiving;
        }

        public async Task PostMessage(NetworkMessage message)
        {
            if (BaseStream == null)
                throw new NullReferenceException("BaseStream is null");

            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is not initialized");

            // Console.WriteLine("Sending {0}", JsonSerializer.Serialize(message));


            var rawMessage = _serialization.Serialize(message);
            var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));

            await BaseStream.WriteAsync(header);
            await BaseStream.WriteAsync(rawMessage);
        }

        public void HandleMessage(NetworkMessage message)
        {
            _messageBuffer.Remove(message);
        }

        public NetworkMessage[] UnhandledMessages => _messageBuffer.ToArray();

        public NetworkMessage? FirstByFilter(Predicate<NetworkMessage> predicate)
        {
            return _messageBuffer.FirstOrDefault(m => predicate(m));
        }

        private async Task<NetworkMessage> GetNextMessage()
        {
            if (BaseStream == null)
                throw new NullReferenceException("BaseStream is null");

            if (_serialization == null)
                throw new NullReferenceException("Serialization toolkit is null");


            // Console.WriteLine("Receiving message");
            var firstBit = (byte)BaseStream.ReadByte();
            var secondBit = (byte)BaseStream.ReadByte();

            var len = BitConverter.ToUInt16(new[] { firstBit, secondBit });
            var localSpan = _buffer[..len];

            await BaseStream.ReadExactlyAsync(localSpan);

            // Console.WriteLine("Receiving {0}", Encoding.Default.GetString(_buffer[..len]));

            var message = _serialization.Deserialize<NetworkMessage>(localSpan.Span);
#if TRACE
            Console.WriteLine($"{DateTime.Now.TimeOfDay} Received Message {message.Id} - {message.SchemaId}");
            TransmissionConfig.TotalTransmittedBytes += len;
            Console.WriteLine($"Total recieced bytes are {TransmissionConfig.TotalTransmittedBytes}");
#endif
            _messageBuffer.Add(message);
            _currentReceiving = Task.Run(async () => await GetNextMessage());

            return message;
        }
    }
}