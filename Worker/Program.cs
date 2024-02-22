using HashCrack.Contracts;
using HashCrack.Worker.Consumers;
using HashCrack.Worker.Service;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
builder.Services.AddTransient<Worker>();

var timeout = int.Parse(configuration["Timeout"] ?? "10000");
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<WorkerJobConsumer>();
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Message<WorkerJob>(c => c.SetEntityName("worker-job"));
        cfg.Message<WorkerJobResult>(c => c.SetEntityName("worker-result"));


        cfg.ReceiveEndpoint("worker-job", e =>
            e.ConfigureConsumer<WorkerJobConsumer>(context));
        cfg.ConfigureEndpoints(context);
    });
});
builder.Build().Run();