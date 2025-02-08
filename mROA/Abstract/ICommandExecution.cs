using global::System;

namespace mROA.Abstract
{
    public interface ICommandExecution
    {
        Guid CallRequestId { get; set; }
        int ClientId { get; set; }
        int CommandId { get; }
    }
}

