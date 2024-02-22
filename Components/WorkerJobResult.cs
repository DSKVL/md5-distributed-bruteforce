namespace HashCrack.Components;

public record WorkerJobResult(
    string Guid,
    Guid JobId,
    Status Status,
    string? Data = null);