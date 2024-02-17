using System.Text.Json.Serialization;
using HashCrack.Enums;

namespace Manager.DTO;

public record CrackStatusResponseDto(
    [property: JsonPropertyName("status")] Status status,
    [property: JsonPropertyName("data")] string[] data);