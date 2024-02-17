using HashCrack.Contracts.DTO;
using Manager;
using Manager.DTO;
using Manager.Service;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.AddWorkersClients();
builder.Services.AddSingleton<WorkerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var crackResponseHandler
    = async ([FromServices] WorkerService workerService, [FromBody] HashCrackRequestDto request)
        => new HashCrackResponseDto(await workerService.CreateTask(request.Hash, request.MaxLength));
var statusResponseHandler
    = ([FromServices] WorkerService workerService, [FromQuery] Guid taskId) =>
    {
        var (status, data) = workerService.CheckStatus(taskId);
        return new CrackStatusResponseDto(status, data);
    };
var internalHashRequest =
    ([FromServices] WorkerService workerService, [FromRoute] Guid requestId,
            [FromBody] CrackWorkerTaskCompletionDto dto)
        => workerService.UpdateTask(requestId, dto.WorkerId, dto.Status, dto.Data);

app.MapPost("/api/hash/crack", crackResponseHandler)
    .WithName("CrackRequest")
    .WithOpenApi();
app.MapGet("/api/hash/status", statusResponseHandler)
    .WithName("CrackStatus")
    .WithOpenApi();
app.MapPatch("/internal/api/manager/hash/crack/request/{requestId}", internalHashRequest);

app.Run();