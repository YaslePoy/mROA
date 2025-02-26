using System;
using System.Text.Json.Serialization;
using mROA.Abstract;
using mROA.Implementation.Attributes;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mROA.Implementation.CommandExecution
{
    public class FinalCommandExecution : ICommandExecution
    {
        public Guid Id { get; set; }
        [SerializationIgnore]
        public int ClientId { get; set; }
        [SerializationIgnore]
        public int CommandId { get; set; }
    }

    public class FinalCommandExecution<T> : FinalCommandExecution
    {
        public T? Result { get; set; }
    }
}