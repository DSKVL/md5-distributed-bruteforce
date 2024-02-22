using HashCrack.Components.Model;
using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace HashCrack.Components.Service;

public class JobSubmitService : IJobSubmitService
{
    private readonly MongoDbContext _dbContext;

    //private readonly IPublishEndpoint _publishEndpoint;
    //private readonly IBus _bus;
    private readonly ILogger<JobSubmitService> _logger;
    private readonly ManagerService _managerService;
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public JobSubmitService(
        ManagerService managerService,
        MongoDbContext dbContext,
        ISendEndpointProvider sendEndpointProvider,
        //IPublishEndpoint publishEndpoint,
        //IBus bus,
        ILogger<JobSubmitService> logger)
    {
        _managerService = managerService;
        _dbContext = dbContext;
        _sendEndpointProvider = sendEndpointProvider;
        //_publishEndpoint = publishEndpoint;
        //_bus = bus;
        _logger = logger;
    }

    public async Task<CrackTask> CreateAndSubmitJobs(string targetHash, uint maxSourceLength)
    {
        var crackTask = _managerService.CreateTask(targetHash, maxSourceLength);
        var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:worker-job"));
        //var sendEndpoint = await _bus.GetSendEndpoint(new Uri("queue:worker-job"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _dbContext.BeginTransaction(cts.Token);

        foreach (var (_, workerCrackTask) in crackTask.WorkerTasks)
        {
            _logger.LogInformation("Send job request with task offset {TaskOffset} and count {SendCount}",
                workerCrackTask.Offset,
                workerCrackTask.SendCount);

            //await _publishEndpoint.Publish(new WorkerJob(
            await sendEndpoint.Send(new WorkerJob(
                crackTask.Id.ToString(),
                workerCrackTask.JobId,
                workerCrackTask.Hash,
                workerCrackTask.Offset,
                workerCrackTask.SendCount,
                workerCrackTask.MaxLength,
                string.Concat(workerCrackTask.Alphabet)), cts.Token);
        }

        //SetTimeout(task, _timeout);
        try
        {
            await _dbContext.CommitTransaction(cts.Token);
        }
        catch (MongoCommandException exception) when (exception.CodeName == "DuplicateKey")
        {
            throw new Exception("Duplicate registration", exception);
        }

        return crackTask;
    }

    private void SetTimeout(CrackTask task, int timeout)
    {
        Task.Delay(timeout).ContinueWith(_ =>
        {
            if (task.Status != Status.Ready)
            {
                task.Status = Status.Error;
            }

            _logger.LogInformation("Timeout. Task status is {Status}", task.Status);
        });
    }
}