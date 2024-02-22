using HashCrack.Contracts;
using MassTransit;

namespace HashCrack.Worker.Consumers;

public class WorkerJobConsumer : IConsumer<WorkerJob>
{
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly Service.Worker _worker;

    public WorkerJobConsumer(Service.Worker worker,
        ISendEndpointProvider sendEndpointProvider)
    {
        _worker = worker;
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task Consume(ConsumeContext<WorkerJob> context)
    {
        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:worker-result"));
        foreach (var result in _worker.Crack(context.Message))
        {
            await sendEndpoint.Send(result);
        }
    }
}