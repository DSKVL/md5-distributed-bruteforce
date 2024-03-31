using MassTransit;

namespace HashCrack.Components.Consumers;

public class WorkerJobConsumerDefinition : ConsumerDefinition<WorkerJobConsumer>
{
    public WorkerJobConsumerDefinition()
    {
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkerJobConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(10, 50, 100, 1000, 1000, 1000, 1000, 1000));

        endpointConfigurator.UseMongoDbOutbox(context);
    }
}