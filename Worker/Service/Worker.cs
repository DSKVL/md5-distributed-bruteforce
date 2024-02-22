using System.Security.Cryptography;
using Combinatorics.Collections;
using HashCrack.Contracts;
using HashCrack.Contracts.Model;

namespace HashCrack.Worker.Service;

public class Worker
{
    private readonly ILogger<Worker> _logger;

    private WorkerCrackTask _task = new()
    {
        MaxLength = 0,
        Alphabet = Array.Empty<char>()
    };

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    private IEnumerable<string> RequestedWordsEnumerator => Enumerable
        .Range(1, (int)_task.MaxLength)
        .Select(lowerIndex => new Variations<char>(_task.Alphabet, lowerIndex, GenerateOption.WithRepetition))
        .Aggregate(Enumerable.Empty<IReadOnlyList<char>>(),
            (accumulatorEnumerable, enumerable) => accumulatorEnumerable.Concat(enumerable))
        .Skip((int)_task.Offset)
        .Take((int)_task.SendCount)
        .Select(variation => string.Concat(variation));

    public IEnumerable<WorkerJobResult> Crack(WorkerJob job)
    {
        _task = JobToTask(job);
        _logger.LogInformation(
            "Started processing hash {ProcessedHash} with task offset {TaskOffset} and count {TaskCount}",
            _task.Hash, _task.Offset, _task.SendCount);

        foreach (var word in RequestedWordsEnumerator)
        {
            _logger.LogTrace("Checking string \"{CheckedString}\"",
                word);

            var hash = CalculateHash(word);
            if (!CheckHashMatch(hash)) continue;

            _logger.LogInformation("Found matching string \"{MatchedString}\" with hash {ProcessedHash}",
                word,
                _task.Hash);

            yield return new WorkerJobResult(job.Guid, _task.WorkerId, Status.InProgress, word);
        }

        _logger.LogInformation("Finished processing hash: {ProcessedHash}", _task.Hash);

        yield return new WorkerJobResult(job.Guid, _task.WorkerId, Status.Ready);
    }

    private static WorkerCrackTask JobToTask(WorkerJob job) => new()
    {
        WorkerId = job.WorkerId,
        Status = job.Status,
        Hash = job.Hash,
        Offset = job.Offset,
        SendCount = job.SendCount,
        MaxLength = job.MaxLength,
        Alphabet = job.Alphabet,
    };

    private bool CheckHashMatch(string hash) => _task.Hash == hash;

    private static string CalculateHash(string word)
    {
        var byteArray = word.Select(Convert.ToByte).ToArray();
        var hash = MD5.HashData(byteArray);
        return HashToString(hash);
    }

    private static string HashToString(byte[] bytes) => string.Concat(bytes.Select(b => b.ToString("X2").ToLower()));
}