using System;
using mROA.Abstract;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.CommandExecution
{
    public class ExceptionCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public EMessageType MessageType => EMessageType.ExceptionCommandExecution;
        public string Exception { get; set; }

        public RemoteException GetException()
        {
            return new RemoteException(Exception) { CallRequestId = Id };
        }
    }
}