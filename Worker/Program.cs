var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var configuration = builder.Configuration;
builder.Services.AddTransient<Worker.Service.Worker>();
builder.Services.AddHttpClient<Worker.Service.Worker>(c =>
{
    c.BaseAddress = new Uri($"http://{configuration["ManagerHostname"]}");
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPatch("/internal/api/manager/hash/crack/request",
        (Worker.Service.Worker worker) => worker.Crack())
    .WithName("HashCrackTaskRequest")
    .WithOpenApi();

app.Run();