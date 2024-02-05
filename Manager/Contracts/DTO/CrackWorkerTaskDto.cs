using System.Text.Json.Serialization;
using Manager.Model;

namespace Manager.Contracts.DTO;

public record CrackWorkerTaskDto(
    [property: JsonPropertyName("status")] Status Status,
    [property: JsonPropertyName("hash")] string? Hash,
    [property: JsonPropertyName("offset")] ulong Offset,
    [property: JsonPropertyName("sendCount")]
    ulong SendCount,
    [property: JsonPropertyName("maxLength")]
    uint MaxLength);