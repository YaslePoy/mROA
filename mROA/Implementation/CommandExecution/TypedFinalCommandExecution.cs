using System;
using System.Text.Json.Serialization;
using mROA.Implementation.Attributes;

namespace mROA.Implementation.CommandExecution
{
    public class TypedFinalCommandExecution : FinalCommandExecution<object>
    {
        [SerializationIgnore]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Type? Type { get; set; }
    }
}