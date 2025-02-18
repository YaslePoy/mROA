using System.Text.Json.Serialization;

namespace mROA.Implementation.CommandExecution;

public class TypedFinalCommandExecution : FinalCommandExecution<object>
{
    [JsonIgnore]
    public Type? Type { get; set; }
}