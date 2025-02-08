using System.Collections.Generic;
using System.Text.Json.Serialization;
using global::System;
using mROA.Abstract;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mROA.Implementation
{
    public class FinalCommandExecution : ICommandExecution
    {
        public Guid CallRequestId { get; set; }
        [JsonIgnore]
        public int ClientId { get; set; }
        [JsonIgnore]
        public int CommandId { get; set; }
    }

    public class FinalCommandExecution<T> : FinalCommandExecution
    {
        public T? Result { get; set; }
    }

    public class TypedFinalCommandExecution : FinalCommandExecution<object>
    {
        [JsonIgnore]
        public Type? Type { get; set; }
    }

    public class ExceptionCommandExecution : ICommandExecution
    {
        public Guid CallRequestId { get; set; }
        public int ClientId { get; set; }
        public int CommandId { get; set; }
        public string Exception { get; set; }
    }
}