using System.Collections.Concurrent;
using HashCrack.Contracts;
using HashCrack.Contracts.Model;

namespace HashCrack.Manager.Model;

public class CrackTask
{
    public readonly IList<string> HashSources = new List<string>();
    public Status Status { get; set; } = Status.InProgress;
    public Guid Id { get; set; } = Guid.NewGuid();

    public IDictionary<Guid, WorkerCrackTask> WorkerTasks { get; init; } =
        new ConcurrentDictionary<Guid, WorkerCrackTask>();
}