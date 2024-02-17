using HashCrack.Contracts.DTO;
using HashCrack.Worker.Service;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
var configuration = builder.Configuration;
builder.Services.AddTransient<Worker>();
builder.Services.AddHttpClient<Worker>(c =>
    c.BaseAddress = new Uri($"http://{configuration["MANAGER_HOSTNAME"]}:8080"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var crackTaskHandler = ([FromServices] Worker worker, [FromRoute] Guid taskId, [FromBody] CrackWorkerTaskDto dto)
    => worker.Crack(new()
    {
        WorkerId = dto.WorkerId,
        Status = dto.Status,
        Hash = dto.Hash,
        Offset = dto.Offset,
        SendCount = dto.SendCount,
        MaxLength = dto.MaxLength,
        Alphabet = dto.Alphabet
    }, taskId);
app.MapPut("/internal/api/worker/request/{taskId}", crackTaskHandler)
    .WithName("HashCrackTaskRequest")
    .WithOpenApi();
app.Run();