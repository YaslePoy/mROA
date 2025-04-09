using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Test
{
    public class NextGenTest
    {
        private TcpListener _listener;
        private NextGenerationInteractionModule _interactionModuleA;
        private NextGenerationInteractionModule _interactionModuleB;
        private Guid[] guids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        [SetUp]
        public void Setup()
        {
            _listener = new TcpListener(IPAddress.Loopback, 4567);
            _interactionModuleA = new NextGenerationInteractionModule();
            _interactionModuleA.Inject(new JsonSerializationToolkit());
            _interactionModuleB = new NextGenerationInteractionModule();
            _interactionModuleB.Inject(new JsonSerializationToolkit());

        }

        [Test]
        public void MultithreadedTest()
        {
        
            Task.Run(() =>
            {
                _listener.Start();
                _interactionModuleB.BaseStream = _listener.AcceptTcpClient().GetStream();

                foreach (var guid in guids)
                {
                    _interactionModuleB.PostMessageAsync(new NetworkMessageHeader { Id = guid, Data = "Hello user"u8.ToArray() });
                }
            });
        
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, 4567);
            _interactionModuleA.BaseStream = client.GetStream();

            var tasks = guids.Select(ReadStream);

            Task.WaitAll(tasks.ToArray());
            Assert.Pass();
        
        }

        private async Task ReadStream(Guid current)
        {
            var msg = await _interactionModuleA.GetNextMessageReceiving();
            Console.WriteLine(
                $"{Environment.CurrentManagedThreadId} Received message: {Encoding.Default.GetString(new JsonSerializationToolkit().Serialize(msg))}");
            while (msg.Id != current)
            {
                msg = await _interactionModuleA.GetNextMessageReceiving();
                Console.WriteLine(
                    $"{Environment.CurrentManagedThreadId} Received message: {Encoding.Default.GetString(new JsonSerializationToolkit().Serialize(msg))}");
            }

            Console.WriteLine($"{Environment.CurrentManagedThreadId} Good message received");
        }

        [TearDown]
        public void TearDown()
        {
            _listener.Stop();
            _listener.Dispose();
            _interactionModuleA.Dispose();
            _interactionModuleB.Dispose();
        }
    }
}