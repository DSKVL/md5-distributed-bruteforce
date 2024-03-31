using HashCrack.Components;
using HashCrack.Components.Consumers;
using HashCrack.Components.Service;
using HashCrack.Manager.DTO;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ManagerService>();
builder.Services.AddScoped<IJobSubmitService, JobSubmitService>();
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
builder.Services.AddSingleton<IMongoDatabase>(provider =>
    provider.GetRequiredService<IMongoClient>().GetDatabase("HashCrackOutbox"));

builder.Services.AddMassTransit(x =>
{
    x.AddMongoDbOutbox(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
        o.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);

        o.UseBusOutbox();
    });
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<WorkerResultConsumer, WorkerResultConsumerDefinition>();
    x.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));
});

EndpointConvention.Map<WorkerJob>(new Uri("queue:worker-job"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

var crackResponseHandler
    = async ([FromServices] IJobSubmitService workerJobService,
        [FromBody] HashCrackRequestDto request) =>
    {
        var task = await workerJobService.CreateAndSubmitJobs(request.Hash, request.MaxLength);
        return new HashCrackResponseDto(task.Id.ToString());
    };
var statusResponseHandler
    = ([FromServices] ManagerService workerService, [FromQuery] Guid taskId) =>
    {
        try
        {
            var (status, data) = workerService.CheckStatus(taskId);
            return Results.Ok(new CrackStatusResponseDto(status, data));
        }
        catch (KeyNotFoundException)
        {
            return Results.StatusCode(418);
        }
    };

app.MapPost("/api/hash/crack", crackResponseHandler)
    .WithName("CrackRequest")
    .WithOpenApi();
app.MapGet("/api/hash/status", statusResponseHandler)
    .WithName("CrackStatus")
    .WithOpenApi();

app.Run();