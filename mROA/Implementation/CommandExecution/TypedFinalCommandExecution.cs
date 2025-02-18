using System.Text.Json.Serialization;

namespace mROA.Implementation.CommandExecution;

public class TypedFinalCommandExecution : FinalCommandExecution<object>
{
    [JsonIgnore]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Type? Type { get; set; }
}