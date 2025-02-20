using System;

namespace mROA.Abstract
{
    public interface ICommandExecution
    {
        Guid Id { get; set; }
        int ClientId { get; set; }
        int CommandId { get; }
    }
}