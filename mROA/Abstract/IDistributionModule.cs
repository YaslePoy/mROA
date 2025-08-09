using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IDistributionModule
    {
        Action<NetworkMessage> GetDistributionAction(int clientId);
    }
}