using HashCrack.Model;

namespace Contracts.Contracts.DTO;

public record WorkerTaskCompletionDto(
    int workerId,
    Status status,
    string? data = null);