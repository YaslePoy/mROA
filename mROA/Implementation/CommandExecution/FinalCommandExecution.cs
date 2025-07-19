using System;
using mROA.Abstract;

namespace mROA.Implementation.CommandExecution
{
    public struct FinalCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public EMessageType MessageType => EMessageType.FinishedCommandExecution;
    }

    public struct FinalCommandExecution<T> : ICommandExecution
    {
        public Guid Id { get; set; }
        public EMessageType MessageType => EMessageType.FinishedCommandExecution;
        public T? Result { get; set; }
    }
}