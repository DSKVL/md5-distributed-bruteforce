using System.Collections.Concurrent;
using HashCrack.Contracts;
using HashCrack.Contracts.Model;
using HashCrack.Manager.Model;
using MassTransit;

namespace HashCrack.Manager.Service;

public class WorkerService
{
    private readonly char[] _alphabet;
    private readonly ILogger<WorkerService> _logger;
    private readonly uint _maxLength;
    private readonly ulong[] _powerTable;
    private readonly IDictionary<Guid, CrackTask> _tasks = new ConcurrentDictionary<Guid, CrackTask>();
    private readonly int _timeout;
    private readonly int _workerCount;

    public WorkerService(
        ILogger<WorkerService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _workerCount = int.Parse(configuration["WORKER_COUNT"] ?? "1");
        _maxLength = uint.Parse(configuration["MaxLength"] ?? "0");
        _alphabet = (configuration["Alphabet"] ?? "").ToCharArray();
        _timeout = int.Parse(configuration["Timeout"] ?? "1000");
        _powerTable = FillPowerTable(_maxLength, (uint)_alphabet.Length);
    }

    private static ulong[] FillPowerTable(uint length, ulong b)
    {
        var powerTable = new ulong[length + 1];
        powerTable[0] = 1;
        for (var i = 1; i <= length; i++)
        {
            powerTable[i] = powerTable[i - 1] * b;
        }

        return powerTable;
    }

    public async Task<string> CreateTask(string targetHash, uint maxSourceLength,
        ISendEndpointProvider sendEndpointProvider)
    {
        var workerTasks = GetWorkerTasks(targetHash, maxSourceLength, _workerCount);
        var task = new CrackTask { WorkerTasks = workerTasks };
        _tasks.Add(task.Id, task);
        var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:worker-job"));

        foreach (var workerCrackTask in workerTasks)
        {
            _logger.LogInformation("Send job request with task offset {TaskOffset} and count {SendCount}",
                workerCrackTask.Offset,
                workerCrackTask.SendCount);

            await sendEndpoint.Send(new WorkerJob(
                task.Id.ToString(),
                workerCrackTask.WorkerId,
                workerCrackTask.Status,
                workerCrackTask.Hash,
                workerCrackTask.Offset,
                workerCrackTask.SendCount,
                workerCrackTask.MaxLength,
                string.Concat(workerCrackTask.Alphabet)));
        }

        SetTimeout(task, _timeout);

        return task.Id.ToString();
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

    public (Status, string[]) CheckStatus(Guid taskId)
        => (_tasks[taskId].Status,
            _tasks[taskId].HashSources.ToArray());

    public void UpdateTask(Guid taskId, int workerId, Status receivedStatus, string? receivedData)
    {
        _logger.LogInformation("Received result with state {ReceivedStatus} and data {ReceivedData}",
            receivedStatus.ToString(),
            receivedData ?? "");
        var task = _tasks[taskId];
        var workerTask = task.WorkerTasks[workerId];
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
                if (task.WorkerTasks.All(wt => wt.Status == Status.Ready))
                {
                    task.Status = Status.Ready;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(receivedStatus), receivedStatus, null);
        }
    }

    private List<WorkerCrackTask> GetWorkerTasks(string targetHash, uint maxSourceLength, int workerCount)
    {
        var possibleSourcesCount = Enumerable
            .Range(1, (int)maxSourceLength)
            .Select(idx => _powerTable[idx])
            .Aggregate(0UL, (a, b) => a + b);
        var sendCounts = GetSendCounts(possibleSourcesCount, (uint)workerCount);
        var offsets = GetOffsets(sendCounts);

        return offsets.Zip(sendCounts).Select(tuple => new WorkerCrackTask
        {
            Hash = targetHash,
            MaxLength = _maxLength,
            Offset = tuple.First,
            SendCount = tuple.Second,
            Alphabet = _alphabet
        }).Select((task, idx) =>
        {
            task.WorkerId = idx;
            return task;
        }).ToList();
    }

    private static List<ulong> GetSendCounts(ulong totalDataCount, uint partsCount)
    {
        var sendCounts = new List<ulong>();
        var extraData = totalDataCount % partsCount;
        var commonData = totalDataCount / partsCount;

        for (var i = 0ul; i < extraData; i++)
        {
            sendCounts.Add(commonData + 1);
        }

        for (var i = extraData; i < partsCount; i++)
        {
            sendCounts.Add(commonData);
        }

        return sendCounts;
    }

    private static List<ulong> GetOffsets(IList<ulong> sendCounts)
    {
        var offsets = Enumerable.Repeat(0ul, sendCounts.Count).ToArray();

        ulong cummulativeSum = 0;
        for (var i = 1; i < sendCounts.Count; i++)
        {
            offsets[i] = cummulativeSum + sendCounts[i - 1];
            cummulativeSum += offsets[i];
        }

        return offsets.ToList();
    }
}