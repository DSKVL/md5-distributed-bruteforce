using MassTransit;

namespace HashCrack.Components.Consumers;

public class WorkerResultConsumerDefinition : ConsumerDefinition<WorkerResultConsumer>
{
    public WorkerResultConsumerDefinition()
    {
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkerResultConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(10, 50, 100, 1000, 1000, 1000, 1000, 1000));

        endpointConfigurator.UseMongoDbOutbox(context);
    }
}