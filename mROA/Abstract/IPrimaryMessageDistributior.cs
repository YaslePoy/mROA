using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IPrimaryMessageDistributior
    {
        Task Distribute(NetworkMessageHeader message);
    }

    public interface IMessageDistributorFactory
    {
        IPrimaryMessageDistributior Produce(int clientId);
    }
}