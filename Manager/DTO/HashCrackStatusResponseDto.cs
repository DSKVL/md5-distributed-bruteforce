using System.Text.Json.Serialization;
using HashCrack.Contracts;

namespace HashCrack.Manager.DTO;

public record CrackStatusResponseDto(
    [property: JsonPropertyName("status")] Status status,
    [property: JsonPropertyName("data")] string[] data);