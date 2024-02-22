using System.Collections.Concurrent;

namespace HashCrack.Components.Model;

public class CrackTask
{
    public readonly IList<string> HashSources = new List<string>();
    public Status Status { get; set; } = Status.InProgress;
    public Guid Id { get; set; } = Guid.NewGuid();

    public IDictionary<Guid, WorkerCrackTask> WorkerTasks { get; init; } =
        new ConcurrentDictionary<Guid, WorkerCrackTask>();
}