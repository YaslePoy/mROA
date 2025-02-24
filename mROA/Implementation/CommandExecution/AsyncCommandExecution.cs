using System;
using mROA.Abstract;

namespace mROA.Implementation.CommandExecution
{
    public class AsyncCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        public int ClientId { get; set; }
        public int CommandId { get; set; }
    }
}