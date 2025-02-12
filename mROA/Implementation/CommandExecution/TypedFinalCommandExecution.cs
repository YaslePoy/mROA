using System.Text.Json.Serialization;

namespace mROA.Implementation;

public class TypedFinalCommandExecution : FinalCommandExecution<object>
{
    [JsonIgnore]
    public Type? Type { get; set; }
}