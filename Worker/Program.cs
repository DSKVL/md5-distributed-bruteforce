using HashCrack.Components;
using HashCrack.Components.Consumers;
using HashCrack.Components.Service;
using MassTransit;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTransient<WorkerService>();

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
builder.Services.AddSingleton<IMongoDatabase>(provider =>
    provider.GetRequiredService<IMongoClient>().GetDatabase("HashCrackOutbox"));

var timeout = int.Parse(builder.Configuration["Timeout"] ?? "10000");
builder.Services.AddMassTransit(x =>
{
    x.AddMongoDbOutbox(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
        o.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
    });
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<WorkerJobConsumer, WorkerJobConsumerDefinition>();
    x.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));
});

EndpointConvention.Map<WorkerJobResult>(new Uri("queue:worker-job-result"));

builder.Build().Run();