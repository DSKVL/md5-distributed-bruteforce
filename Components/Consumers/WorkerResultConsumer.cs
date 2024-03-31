using MassTransit;

namespace HashCrack.Components.Consumers;

public interface ISecondBus :
    IBus
{
}

public class WorkerResultConsumer : IConsumer<WorkerJobResult>
{
    private readonly Service.ManagerService _managerService;

    public WorkerResultConsumer(Service.ManagerService managerService)
    {
        _managerService = managerService;
    }

    public Task Consume(ConsumeContext<WorkerJobResult> context)
    {
        var message = context.Message;
        _managerService.UpdateTask(
            Guid.Parse(message.Guid),
            message.JobId,
            message.Status,
            message.Data);
        return Task.CompletedTask;
    }
}