using System;
using System.Text.Json.Serialization;
// ReSharper disable UnusedMember.Global

namespace mROA.Implementation
{
    public class NetworkMessage
    {
        public Guid Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageType SchemaId { get; set; }

        public byte[] Data { get; set; }
    }

    public enum MessageType
    {
        Unknown, FinishedCommandExecution, ExceptionCommandExecution, AsyncCancelCommandExecution, CallRequest, IdAssigning 
    }
}