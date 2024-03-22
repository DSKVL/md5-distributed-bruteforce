using HashCrack.Contracts;
using HashCrack.Manager;
using HashCrack.Manager.Consumers;
using HashCrack.Manager.DTO;
using HashCrack.Manager.Service;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<WorkerService>();

var connectionString = builder.Configuration.GetConnectionString("Default");
const string databaseName = "HashCrackOutbox";

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
builder.Services.AddSingleton<IMongoDatabase>(provider =>
    provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
builder.Services.AddMongoDbCollection<WorkerJob>(x => x.JobId);
builder.Services.AddMongoDbCollection<WorkerJobResult>(x => x.JobId);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<WorkerResultConsumer>();
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Message<WorkerJob>(c => c.SetEntityName("worker-job"));
        cfg.Message<WorkerJobResult>(c => c.SetEntityName("worker-result"));

        cfg.ReceiveEndpoint("worker-result", e =>
            e.ConfigureConsumer<WorkerResultConsumer>(context));

        cfg.ConfigureEndpoints(context);
    });

    x.AddMongoDbOutbox(o =>
    {
        o.DisableInboxCleanupService();
        o.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
        o.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());

        o.UseBusOutbox(bo => bo.DisableDeliveryService());
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var crackResponseHandler
    = async ([FromServices] WorkerService workerService,
        [FromServices] ISendEndpointProvider clientProvider,
        [FromBody] HashCrackRequestDto request) =>
    {
        var taskId = await workerService.CreateTask(request.Hash, request.MaxLength, clientProvider);
        return new HashCrackResponseDto(taskId);
    };
var statusResponseHandler
    = ([FromServices] WorkerService workerService, [FromQuery] Guid taskId) =>
    {
        var (status, data) = workerService.CheckStatus(taskId);
        return new CrackStatusResponseDto(status, data);
    };

app.MapPost("/api/hash/crack", crackResponseHandler)
    .WithName("CrackRequest")
    .WithOpenApi();
app.MapGet("/api/hash/status", statusResponseHandler)
    .WithName("CrackStatus")
    .WithOpenApi();

app.Run();