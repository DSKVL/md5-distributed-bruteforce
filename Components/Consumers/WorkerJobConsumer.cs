using MassTransit;

namespace HashCrack.Components.Consumers;

public class WorkerJobConsumer : IConsumer<WorkerJob>
{
    private readonly Service.WorkerService _workerService;

    public WorkerJobConsumer(Service.WorkerService workerService)
    {
        _workerService = workerService;
    }

    public async Task Consume(ConsumeContext<WorkerJob> context)
    {
        foreach (var result in _workerService.Crack(context.Message))
        {
            await context.Publish(result);
        }
    }
}