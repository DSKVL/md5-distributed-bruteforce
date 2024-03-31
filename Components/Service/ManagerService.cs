using HashCrack.Components.Model;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace HashCrack.Components.Service;

public class ManagerService
{
    private readonly string _alphabet;
    private readonly IMongoCollection<WorkerJob> _jobs;
    private readonly ILogger<ManagerService> _logger;
    private readonly uint _maxLength;
    private readonly long[] _powerTable;
    private readonly IMongoCollection<CrackTask> _tasks;
    private readonly int _workerCount;

    public ManagerService(
        IMongoCollection<CrackTask> tasks,
        IMongoCollection<WorkerJob> jobs,
        ILogger<ManagerService> logger,
        IConfiguration configuration)
    {
        _tasks = tasks;
        _jobs = jobs;
        _logger = logger;
        _workerCount = int.Parse(configuration["WORKER_COUNT"] ?? "1");
        _maxLength = uint.Parse(configuration["MaxLength"] ?? "0");
        _alphabet = configuration["Alphabet"] ?? "";
        _powerTable = FillPowerTable(_maxLength, (uint)_alphabet.Length);
    }

    private static long[] FillPowerTable(uint length, long b)
    {
        var powerTable = new long[length + 1];
        powerTable[0] = 1;
        for (var i = 1; i <= length; i++)
        {
            powerTable[i] = powerTable[i - 1] * b;
        }

        return powerTable;
    }

    public async Task<(CrackTask, IEnumerable<WorkerJob>)> CreateTask(string targetHash, uint maxSourceLength)
    {
        var task = new CrackTask(NewId.NextGuid(), new List<string>());
        var jobs = GetWorkerJobs(task.Id, targetHash, maxSourceLength, _workerCount).ToList();
        await _tasks.InsertOneAsync(task);
        await _jobs.InsertManyAsync(jobs);

        return (task, jobs);
    }

    public (Status, string[]) CheckStatus(Guid taskId)
    {
        var task = GetTask(taskId);
        return (task.Status, task.HashSources.ToArray());
    }

    public void SetTimeout(Guid taskId, int timeout)
    {
        Task.Delay(timeout).ContinueWith(async _ =>
        {
            var task = GetTask(taskId);

            if (task.Status != Status.Ready)
            {
                await UpdateStatus(taskId, Status.Error);
            }

            _logger.LogInformation("Timeout. Task status is {Status}", task.Status);
        });
    }

    public async Task UpdateTask(Guid taskId, Guid jobId, Status receivedStatus, string? receivedData)
    {
        _logger.LogInformation("Received result with state {ReceivedStatus} and data {ReceivedData}",
            receivedStatus.ToString(), receivedData ?? "");

        await UpdateWorkTaskStatus(jobId, receivedStatus);

        var updateReadiness = receivedStatus switch
        {
            Status.InProgress => UpdateData(taskId, receivedData!),
            Status.Error => UpdateStatus(taskId, Status.Error),
            Status.Ready => UpdateTaskReadiness(taskId),
            _ => throw new ArgumentOutOfRangeException(nameof(receivedStatus), receivedStatus, null)
        };

        await updateReadiness;
    }

    private async Task UpdateWorkTaskStatus(Guid jobId, Status newStatus)
    {
        var workerTask = await GetJob(jobId);
        if (workerTask.Status == Status.InProgress)
        {
            var update = Builders<WorkerJob>.Update
                .Set(job => job.Status, newStatus);
            await _jobs.UpdateOneAsync(x => x.JobId == jobId, update);
        }
    }

    private CrackTask GetTask(Guid taskId)
        => _tasks.FindAsync(Builders<CrackTask>
                .Filter
                .Eq(x => x.Id, taskId))
            .Result.First();

    private async Task<IAsyncCursor<WorkerJob>> GetJobs(Guid taskId)
        => await _jobs.FindAsync(Builders<WorkerJob>
            .Filter
            .Eq(x => x.TaskId, taskId));

    private Task<WorkerJob> GetJob(Guid jobId)
        => Task.FromResult(_jobs.FindAsync(Builders<WorkerJob>
                .Filter
                .Eq(x => x.JobId, jobId))
            .Result.First());

    private async Task UpdateData(Guid taskId, string data)
    {
        var filter = Builders<CrackTask>.Filter
            .Eq(x => x.Id, taskId);
        var update = Builders<CrackTask>.Update
            .AddToSet(restaurant => restaurant.HashSources, data);
        await _tasks.UpdateOneAsync(filter, update);
    }

    private async Task UpdateStatus(Guid taskId, Status status)
    {
        var filter = Builders<CrackTask>.Filter
            .Eq(x => x.Id, taskId);
        var update = Builders<CrackTask>.Update
            .Set(restaurant => restaurant.Status, status);
        await _tasks.UpdateOneAsync(filter, update);
    }

    private async Task UpdateTaskReadiness(Guid taskId)
    {
        var jobsCursor = await GetJobs(taskId);
        var jobs = await jobsCursor.ToListAsync();
        if (jobs.All(j => j.Status == Status.Ready))
        {
            await UpdateStatus(taskId, Status.Ready);
        }
    }

    private IEnumerable<WorkerJob> GetWorkerJobs(Guid taskId, string targetHash, uint maxSourceLength, int workerCount)
    {
        var possibleSourcesCount = GetPossibleSourcesCount(maxSourceLength);
        var sendCounts = GetSendCounts(possibleSourcesCount, workerCount);
        var offsets = GetOffsets(sendCounts);
        return offsets.Zip(sendCounts)
            .Select(tuple => new WorkerJob(
                TaskId: taskId,
                JobId: NewId.NextGuid(),
                Hash: targetHash,
                Offset: (uint)tuple.First,
                SendCount: (uint)tuple.Second,
                MaxLength: _maxLength,
                Alphabet: _alphabet
            ));
    }

    private long GetPossibleSourcesCount(uint maxSourceLength)
        => Enumerable
            .Range(1, (int)maxSourceLength)
            .Select(idx => _powerTable[idx])
            .Sum();

    private static List<long> GetSendCounts(long totalDataCount, int partsCount)
    {
        var sendCounts = new List<long>();
        var extraData = totalDataCount % partsCount;
        var commonData = totalDataCount / partsCount;

        for (var i = 0; i < extraData; i++)
        {
            sendCounts.Add(commonData + 1);
        }

        for (var i = extraData; i < partsCount; i++)
        {
            sendCounts.Add(commonData);
        }

        return sendCounts;
    }

    private static List<long> GetOffsets(IList<long> sendCounts)
    {
        var offsets = Enumerable.Repeat(0L, sendCounts.Count).ToArray();

        long cummulativeSum = 0;
        for (var i = 1; i < sendCounts.Count; i++)
        {
            offsets[i] = cummulativeSum + sendCounts[i - 1];
            cummulativeSum += offsets[i];
        }

        return offsets.ToList();
    }
}