namespace HashCrack.Components.Model;

public class WorkerCrackTask
{
    public Guid JobId { get; set; }
    public Status Status { get; set; } = Status.InProgress;
    public string? Hash { get; init; }
    public ulong Offset { get; init; }
    public ulong SendCount { get; init; }
    public uint MaxLength { get; init; }
    public IEnumerable<char> Alphabet { get; init; } = Enumerable.Empty<char>();
}