using System;
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace mROA.Implementation
{
    public interface ICallRequest
    {
        Guid Id { get; }
        int CommandId { get; }
        int ObjectId { get; }
        object? Parameter { get; }
    }

    public class DefaultCallRequest : ICallRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int CommandId { get; set; }
        public int ObjectId { get; set; } = -1;
    
        [JsonIgnore]
        public Type? ParameterType { get; set; }
        public object? Parameter { get; set; }
    }
}