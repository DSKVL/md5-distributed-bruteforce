using System.Text.Json.Serialization;
using HashCrack.Enums;

namespace HashCrack.Contracts.DTO;

public record CrackWorkerTaskDto(
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