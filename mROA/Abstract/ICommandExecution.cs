using System;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface ICommandExecution : INetworkMessage
    {
        Guid Id { get; set; }
    }
}