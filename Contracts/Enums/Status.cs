using System.Text.Json.Serialization;

namespace HashCrack.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status
{
    Error = -1,
    InProgress,
    Ready,
}