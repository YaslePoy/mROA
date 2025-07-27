using System;
using mROA.Abstract;

namespace mROA.Implementation.CommandExecution
{
    public class AsyncCommandExecution : ICommandExecution
    {
        public RequestId Id { get; set; }
        public EMessageType MessageType => EMessageType.Unknown;
    }
}