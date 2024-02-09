namespace HashCrack.Model;

public class WorkerCrackTask
{
    public int workerId { get; set; }
    public Status Status { get; set; } = Status.IN_PROGRESS;
    public string? Hash { get; init; }
    public ulong Offset { get; init; }
    public ulong SendCount { get; init; }
    public uint MaxLength { get; init; }
    public IEnumerable<char> Alphabet { get; init; }
}