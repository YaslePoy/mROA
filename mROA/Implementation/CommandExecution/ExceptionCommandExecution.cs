using System;
using mROA.Abstract;
using mROA.Implementation.Frontend;

namespace mROA.Implementation.CommandExecution
{
    public class ExceptionCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public int ClientId { get; set; }
        public int CommandId { get; set; }
        public string Exception { get; set; }

        public RemoteException GetException()
        {
            return new RemoteException(Exception) { CallRequestId = Id };
        }
    }

    public class AsyncCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public int ClientId { get; set; }
        public int CommandId { get; set; }
    }
}