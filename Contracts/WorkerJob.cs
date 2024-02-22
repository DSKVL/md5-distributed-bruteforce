using System.Text.Json.Serialization;

namespace HashCrack.Contracts;

public record WorkerJob(
    [property: JsonPropertyName("guid")] string Guid,
    [property: JsonPropertyName("workerId")]
    int WorkerId,
    [property: JsonPropertyName("status")] Status Status,
    [property: JsonPropertyName("hash")] string? Hash,
    [property: JsonPropertyName("offset")] ulong Offset,
    [property: JsonPropertyName("sendCount")]
    ulong SendCount,
    [property: JsonPropertyName("maxLength")]
    uint MaxLength,
    [property: JsonPropertyName("alphabet")]
    string Alphabet);