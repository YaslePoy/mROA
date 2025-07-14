using System.Threading.Channels;
using System.Threading.Tasks;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ChannelDistributorFactory : IMessageDistributorFactory
    {
        private readonly IConnectionHub _hub;

        public ChannelDistributorFactory(IConnectionHub hub)
        {
            _hub = hub;
        }
        public IPrimaryMessageDistributior Produce(int clientId)
        {
            return new ChannelMessageDistributor(_hub.GetInteraction(clientId).ReceiveChanel.Writer);
        }
    }

    public class ChannelMessageDistributor : IPrimaryMessageDistributior
    {
        private readonly ChannelWriter<NetworkMessageHeader> _writer;

        public ChannelMessageDistributor(ChannelWriter<NetworkMessageHeader> writer)
        {
            _writer = writer;
        }

        public async Task Distribute(NetworkMessageHeader message)
        {
            await _writer.WriteAsync(message);
        }
    }
}