using System;
using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.CommandExecution
{
    public class ExceptionCommandExecution : ICommandExecution
    {
        public RequestId Id { get; set; }
        public EMessageType MessageType => EMessageType.ExceptionCommandExecution;
        public string Exception { get; set; }

        public RemoteException GetException()
        {
            return new RemoteException(Exception) { CallRequestId = Id };
        }
    }
}