﻿using System.Text.Json;
using mROA.Abstract;

namespace mROA.Implementation;

public class NetworkMessage
{
    public Guid Id { get; set; }
    public MessageType SchemaId { get; set; }
    public byte[] Data { get; set; }
}

public enum MessageType
{
    Unknown, FinishedCommandExecution, ExceptionCommandExecution, AcyncCancelCommandExecution, CallRequest, IdAssigning 
}