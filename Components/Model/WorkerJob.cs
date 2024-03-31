namespace HashCrack.Components.Model;

public record WorkerJob(Guid TaskId,
    Guid JobId,
    string? Hash,
    ulong Offset,
    ulong SendCount,
    uint MaxLength,
    string Alphabet,
    Status Status = Status.InProgress);