using System.Text.Json.Serialization;

namespace HashCrack.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status
{
    Error = -1,
    InProgress,
    Ready,
}