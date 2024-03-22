using System.Text.Json.Serialization;

namespace HashCrack.Contracts;

public record WorkerJob(
    [property: JsonPropertyName("requestGuid")]
    string RequestGuid,
    [property: JsonPropertyName("jobId")] Guid JobId,
    [property: JsonPropertyName("status")] Status Status,
    [property: JsonPropertyName("hash")] string? Hash,
    [property: JsonPropertyName("offset")] ulong Offset,
    [property: JsonPropertyName("sendCount")]
    ulong SendCount,
    [property: JsonPropertyName("maxLength")]
    uint MaxLength,
    [property: JsonPropertyName("alphabet")]
    string Alphabet);