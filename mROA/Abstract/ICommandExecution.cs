using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface ICommandExecution : INetworkMessage
    {
        RequestId Id { get; set; }
    }
}