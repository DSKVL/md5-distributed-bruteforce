namespace Manager.Model;

public class WorkerCrackTask
{
    public Status Status { get; set; } = Status.IN_PROGRESS;
    public string? Hash { get; init; }
    public ulong Offset { get; init; }
    public ulong SendCount { get; init; }
    public uint MaxLength { get; init; }
}