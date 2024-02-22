namespace HashCrack.Contracts;

public record WorkerJobResult(
    string Guid,
    int WorkerId,
    Status Status,
    string? Data = null);