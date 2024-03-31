using HashCrack.Components.Model;
using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace HashCrack.Components.Service;

public class JobSubmitService : IJobSubmitService
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<JobSubmitService> _logger;
    private readonly ManagerService _managerService;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly int _timeout;

    public JobSubmitService(
        ManagerService managerService,
        MongoDbContext dbContext,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<JobSubmitService> logger,
        IConfiguration configuration)
    {
        _managerService = managerService;
        _dbContext = dbContext;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
        _timeout = int.Parse(configuration["Timeout"] ?? "1000");
    }

    public async Task<CrackTask> CreateAndSubmitJobs(string targetHash, uint maxSourceLength)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _dbContext.BeginTransaction(cts.Token);

        var (task, jobs) = await _managerService.CreateTask(targetHash, maxSourceLength);

        foreach (var job in jobs)
        {
            _logger.LogInformation("Send job request with task offset {TaskOffset} and count {SendCount}",
                job.Offset, job.SendCount);

            await _sendEndpointProvider.Send(job, cts.Token);
        }

        try
        {
            await _dbContext.CommitTransaction(cts.Token);
            _managerService.SetTimeout(task.Id, _timeout);
        }
        catch (MongoCommandException exception) when (exception.CodeName == "DuplicateKey")
        {
            throw new Exception("Duplicate registration", exception);
        }

        return task;
    }
}