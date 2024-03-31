namespace HashCrack.Components.Model;

public record WorkerJobResult(
    Guid Guid,
    Guid JobId,
    Status Status,
    string? Data = null);