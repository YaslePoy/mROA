namespace mROA.Implementation
{
    public enum EMessageType
    {
        Unknown,
        FinishedCommandExecution,
        ExceptionCommandExecution,
        CallRequest,
        IdAssigning,
        CancelRequest,
        EventRequest,
        ClientRecovery,
        ClientConnect
    }
}