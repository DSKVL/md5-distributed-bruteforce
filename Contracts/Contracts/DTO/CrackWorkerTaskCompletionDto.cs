using HashCrack.Enums;

namespace HashCrack.Contracts.DTO;

public record CrackWorkerTaskCompletionDto(
    int WorkerId,
    Status Status,
    string? Data = null);