using System.Collections.Concurrent;
using HashCrack.Contracts.DTO;
using HashCrack.Enums;
using Manager.Model;

namespace Manager.Service;

public class WorkerService
{
    private readonly char[] _alphabet;
    private readonly IList<HttpClient> _httpClients;
    private readonly ILogger<WorkerService> _logger;
    private readonly uint _maxLength;
    private readonly ulong[] _powerTable;
    private readonly IDictionary<Guid, CrackTask> _tasks = new ConcurrentDictionary<Guid, CrackTask>();
    private readonly int _timeout;

    public WorkerService(IHttpClientFactory httpClientFactory,
        ILogger<WorkerService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClients = httpClientFactory.GetWorkersClient(configuration, logger);
        _logger.LogInformation("ConnectionSucceeded");
        _maxLength = uint.Parse(configuration["MaxLength"] ?? "0");
        _alphabet = (configuration["Alphabet"] ?? "").ToCharArray();
        _powerTable = new ulong[_maxLength + 1];
        _powerTable[0] = 1;
        _timeout = int.Parse(configuration["Timeout"] ?? "1000");
        for (var i = 1; i <= _maxLength; i++)
        {
            _powerTable[i] = _powerTable[i - 1] * (ulong)_alphabet.Length;
        }
    }

    public async Task<string> CreateTask(string targetHash, uint maxSourceLength)
    {
        CrackTask task = new()
        {
            WorkerTasks = GetWorkerTasks(targetHash, maxSourceLength, _httpClients.Count)
        };
        _tasks.Add(task.Id, task);

        var workerTasks = task.WorkerTasks;
        foreach (var (httpClient, workerCrackTask) in _httpClients.Zip(workerTasks))
        {
            var requestUri = $"/internal/api/worker/request/{task.Id}";
            _logger.LogInformation("Send request to {WorkerUri} with task offset {TaskOffset} and count {SendCount}",
                requestUri,
                workerCrackTask.Offset,
                workerCrackTask.SendCount);
            httpClient.PutAsync(requestUri, JsonContent.Create(new CrackWorkerTaskDto(
                workerCrackTask.WorkerId,
                workerCrackTask.Status,
                workerCrackTask.Hash,
                workerCrackTask.Offset,
                workerCrackTask.SendCount,
                workerCrackTask.MaxLength,
                string.Concat(workerCrackTask.Alphabet) ?? ""
            )));
        }

        SetTimeout(task, _timeout);

        return task.Id.ToString();
    }

    private static void SetTimeout(CrackTask task, int timeout)
    {
        Task.Delay(timeout).ContinueWith(_ =>
        {
            if (task.Status != Status.Ready)
            {
                task.Status = Status.Error;
            }
        });
    }

    public (Status, string[]) CheckStatus(Guid taskId)
    {
        var status = _tasks[taskId].Status;
        var data = _tasks[taskId].HashSources.ToArray();
        return (status, data);
    }

    public void UpdateTask(Guid taskId, int workerId, Status receivedStatus, string? receivedData)
    {
        var task = _tasks[taskId];
        var workerTask = task.WorkerTasks[workerId];
        if (workerTask.Status == Status.InProgress)
        {
            workerTask.Status = receivedStatus;
        }

        switch (receivedStatus)
        {
            case Status.InProgress:
                task.HashSources.Add(receivedData!);
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