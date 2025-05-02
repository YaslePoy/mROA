using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class StreamExtractor
    {
        private readonly Stream _ioStream;
        private readonly ISerializationToolkit _serializationToolkit;
        private const int BufferSize = ushort.MaxValue;
        private readonly Memory<byte> _buffer = new byte[BufferSize];
        private bool _manualConnectionState = true;

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
            var len = ReadMessageLength();
            var localSpan = _buffer[..len];

            await _ioStream.ReadExactlyAsync(localSpan, cancellationToken: Token);

            var message = _serializationToolkit.Deserialize<NetworkMessageHeader>(localSpan.Span);
#if TRACE
            Console.WriteLine($"{DateTime.Now.TimeOfDay} Received Message {message.Id} - {message.MessageType}");
            TransmissionConfig.TotalTransmittedBytes += len;
            Console.WriteLine($"Total received bytes are {TransmissionConfig.TotalTransmittedBytes}");
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

        public async Task Send(NetworkMessageHeader message, CancellationToken token = default)
        {
            var rawMessage = _serializationToolkit.Serialize(message);
            var header = BitConverter.GetBytes((ushort)rawMessage.Length).AsMemory(0, sizeof(ushort));

#if TRACE
            Console.WriteLine($"{DateTime.Now.TimeOfDay} Posting Message {message.Id} - {message.MessageType}");
            TransmissionConfig.TotalTransmittedBytes += rawMessage.Length;
            Console.WriteLine($"Total received bytes are {TransmissionConfig.TotalTransmittedBytes}");
#endif

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