namespace mROA.Implementation
{
    public enum EMessageType : byte
    {
        Unknown,
        FinishedCommandExecution,
        ExceptionCommandExecution,
        CallRequest,
        IdAssigning,
        CancelRequest,
        EventRequest,
        ClientRecovery,
        ClientConnect,
        ClientDisconnect,
        UntrustedConnect
    }
}