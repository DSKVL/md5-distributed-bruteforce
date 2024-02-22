using HashCrack.Contracts;
using HashCrack.Contracts.Model;

namespace HashCrack.Manager.Model;

public class CrackTask
{
    public readonly IList<string> HashSources = new List<string>();
    public Status Status { get; set; } = Status.InProgress;
    public Guid Id { get; set; } = Guid.NewGuid();
    public IList<WorkerCrackTask> WorkerTasks { get; init; } = new List<WorkerCrackTask>();
}