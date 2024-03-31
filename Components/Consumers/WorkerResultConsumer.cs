using HashCrack.Components.Model;
using MassTransit;

namespace HashCrack.Components.Consumers;

public class WorkerResultConsumer : IConsumer<WorkerJobResult>
{
    private readonly Service.ManagerService _managerService;

    public WorkerResultConsumer(Service.ManagerService managerService)
    {
        _managerService = managerService;
    }

    public async Task Consume(ConsumeContext<WorkerJobResult> context)
    {
        var message = context.Message;
        await _managerService.UpdateTask(
            message.Guid,
            message.JobId,
            message.Status,
            message.Data);
    }
}