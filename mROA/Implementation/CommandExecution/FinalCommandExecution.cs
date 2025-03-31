using System;
using mROA.Abstract;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mROA.Implementation.CommandExecution
{
    public class FinalCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public EMessageType MessageType => EMessageType.FinishedCommandExecution;
    }

    public class FinalCommandExecution<T> : FinalCommandExecution
    {
        public T? Result { get; set; }
    }
}