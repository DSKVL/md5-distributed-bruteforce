using System.Collections.Concurrent;
using HashCrack.Components.Model;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HashCrack.Components.Service;

public class ManagerService
{
    private readonly char[] _alphabet;
    private readonly ILogger<ManagerService> _logger;
    private readonly uint _maxLength;
    private readonly long[] _powerTable;
    private readonly IDictionary<Guid, CrackTask> _tasks = new ConcurrentDictionary<Guid, CrackTask>();
    private readonly int _timeout;
    private readonly int _workerCount;

    public ManagerService(
        ILogger<ManagerService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _workerCount = int.Parse(configuration["WORKER_COUNT"] ?? "1");
        _maxLength = uint.Parse(configuration["MaxLength"] ?? "0");
        _alphabet = (configuration["Alphabet"] ?? "").ToCharArray();
        _timeout = int.Parse(configuration["Timeout"] ?? "1000");
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

    public CrackTask CreateTask(string targetHash, uint maxSourceLength)
    {
        var workerTasks = GetWorkerTasks(targetHash, maxSourceLength, _workerCount);
        var task = new CrackTask { WorkerTasks = workerTasks };
        _tasks.Add(task.Id, task);

        return task;
    }

    public (Status, string[]) CheckStatus(Guid taskId)
        => (_tasks[taskId].Status,
            _tasks[taskId].HashSources.ToArray());

    public void UpdateTask(Guid taskId, Guid jobId, Status receivedStatus, string? receivedData)
    {
        _logger.LogInformation("Received result with state {ReceivedStatus} and data {ReceivedData}",
            receivedStatus.ToString(),
            receivedData ?? "");
        var task = _tasks[taskId];
        var workerTask = task.WorkerTasks[jobId];
        if (workerTask.Status == Status.InProgress)
        {
            workerTask.Status = receivedStatus;
        }

        switch (receivedStatus)
        {
            case Status.InProgress:
                lock (task.HashSources)
                {
                    task.HashSources.Add(receivedData!);
                }

                break;
            case Status.Error:
                task.Status = Status.Error;
                Console.WriteLine("Worker returned error code.");
                break;
            case Status.Ready:
                if (task.WorkerTasks.Values.All(wt => wt.Status == Status.Ready))
                {
                    task.Status = Status.Ready;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(receivedStatus), receivedStatus, null);
        }
    }

    private ConcurrentDictionary<Guid, WorkerCrackTask>
        GetWorkerTasks(string targetHash, uint maxSourceLength, int workerCount)
    {
        var possibleSourcesCount = GetPossibleSourcesCount(maxSourceLength);
        var sendCounts = GetSendCounts(possibleSourcesCount, workerCount);
        var offsets = GetOffsets(sendCounts);
        var workerTasks = offsets.Zip(sendCounts)
            .Select(tuple => new WorkerCrackTask
            {
                Hash = targetHash,
                JobId = NewId.NextGuid(),
                MaxLength = _maxLength,
                Offset = (uint)tuple.First,
                SendCount = (uint)tuple.Second,
                Alphabet = _alphabet
            })
            .Select(t => new KeyValuePair<Guid, WorkerCrackTask>(t.JobId, t));
        return new ConcurrentDictionary<Guid, WorkerCrackTask>(workerTasks);
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