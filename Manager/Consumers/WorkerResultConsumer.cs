using HashCrack.Contracts;
using HashCrack.Manager.Service;
using MassTransit;

namespace HashCrack.Manager.Consumers;

public class WorkerResultConsumer : IConsumer<WorkerJobResult>
{
    private readonly WorkerService _workerService;

    public WorkerResultConsumer(WorkerService workerService)
    {
        _workerService = workerService;
    }

    public async Task Consume(ConsumeContext<WorkerJobResult> context)
    {
        var message = context.Message;
        _workerService.UpdateTask(
            Guid.Parse(message.Guid),
            message.JobId,
            message.Status,
            message.Data);
    }
}