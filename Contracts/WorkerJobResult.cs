namespace HashCrack.Contracts;

public record WorkerJobResult(
    string Guid,
    Guid JobId,
    Status Status,
    string? Data = null);