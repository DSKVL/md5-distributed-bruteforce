using System.Collections.Concurrent;
using Manager.Contracts.DTO;
using Manager.Model;

namespace Manager.Service;

public class WorkerService
{
    private readonly uint _alphabetSize;
    private readonly uint _maxLength;
    private readonly ulong[] _powerTable;
    private List<HttpClient> _httpClients = new List<HttpClient>();
    private IDictionary<Guid, CrackTask> _tasks = new ConcurrentDictionary<Guid, CrackTask>();

    public WorkerService(IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClients.Add(httpClientFactory.CreateClient());
        _maxLength = uint.Parse(configuration["MaxLength"]);
        _alphabetSize = (uint)configuration["Alphabet"].Length;
        _powerTable = new ulong[_maxLength + 1];
        _powerTable[0] = 1;
        for (var i = 1; i <= _maxLength; i++)
        {
            _powerTable[i] = _powerTable[i - 1] * _alphabetSize;
        }
    }

    public async Task<string> CreateTask(string targetHash, uint maxSourceLength)
    {
        CrackTask task = new();
        _tasks.Add(task.Id, task);

        var workerTasks = GetWorkerTasks(targetHash, maxSourceLength, _httpClients.Count);
        foreach (var (httpClient, workerCrackTask) in _httpClients.Zip(workerTasks))
        {
            await httpClient.PostAsync("/internal/api/manager/hash/crack/request",
                JsonContent.Create(new CrackWorkerTaskDto(
                    workerCrackTask.Status,
                    workerCrackTask.Hash,
                    workerCrackTask.Offset,
                    workerCrackTask.SendCount,
                    workerCrackTask.MaxLength)));
        }

        return task.Id.ToString();
    }

    private List<WorkerCrackTask> GetWorkerTasks(string targetHash, uint maxSourceLength, int workerCount)
    {
        var possibleSourcesCount = Enumerable
            .Range(0, (int)maxSourceLength)
            .Select(idx => _powerTable[idx])
            .Aggregate(0UL, (a, b) => a + b);
        var sendCounts = GetSendCounts(possibleSourcesCount, (uint)workerCount);
        var offsets = GetOffsets(sendCounts);

        return offsets.Zip(sendCounts).Select(tuple => new WorkerCrackTask
        {
            Hash = targetHash,
            MaxLength = _maxLength,
            Offset = tuple.First,
            SendCount = tuple.Second
        }).ToList();
    }

    private static List<ulong> GetSendCounts(ulong totalDataCount, uint partsCount)
    {
        var sendCounts = new List<ulong>();
        var extraData = totalDataCount % partsCount;
        var commonData = totalDataCount / partsCount;

        for (var i = 0ul; i < extraData; i++)
        {
            sendCounts.Add((commonData + 1));
        }

        for (var i = extraData; i < partsCount; i++)
        {
            sendCounts.Add(commonData);
        }

        return sendCounts;
    }

    private static List<ulong> GetOffsets(IList<ulong> sendCounts)
    {
        var offsets = new List<ulong>(sendCounts);

        ulong cummulativeSum = 0;
        offsets.ForEach(offset =>
        {
            offset += cummulativeSum;
            cummulativeSum = offset;
        });
        return offsets;
    }
}