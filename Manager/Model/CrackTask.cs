namespace Manager.Model;

public class CrackTask
{
    public Status Status { get; set; } = Status.IN_PROGRESS;
    public Guid Id { get; set; } = Guid.NewGuid();
    public ICollection<WorkerCrackTask> WorkerTasks { get; set; } = new List<WorkerCrackTask>();
}