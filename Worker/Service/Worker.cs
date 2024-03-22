using System.Security.Cryptography;
using Combinatorics.Collections;
using HashCrack.Contracts.DTO;
using HashCrack.Enums;

namespace HashCrack.Worker.Service;

using VariationsEnumerator = IEnumerable<IReadOnlyList<char>>;

public class Worker
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _managerHttpClient;

    private WorkerCrackTask _task = new()
    {
        MaxLength = 0,
        Alphabet = Array.Empty<char>()
    };

    private Guid _taskId;

    public Worker(ILogger<Worker> logger, HttpClient managerHttpClient)
    {
        _logger = logger;
        _managerHttpClient = managerHttpClient;
    }

    private VariationsEnumerator RequestedStringsEnumerator => Enumerable
        .Range(1, (int)_task.MaxLength)
        .Select(lowerIndex => new Variations<char>(_task.Alphabet, lowerIndex, GenerateOption.WithRepetition))
        .Aggregate(Enumerable.Empty<IReadOnlyList<char>>(),
            (accumulatorEnumerable, enumerable) => accumulatorEnumerable.Concat(enumerable))
        .Skip((int)_task.Offset)
        .Take((int)_task.SendCount);

    public async Task Crack(WorkerCrackTask task, Guid taskId)
    {
        _task = task;
        _taskId = taskId;
        _logger.LogInformation(
            "Started processing hash {ProcessedHash} with task offset {TaskOffset} and count {TaskCount}",
            task.Hash, task.Offset, task.SendCount);
        foreach (var variation in RequestedStringsEnumerator)
        {
            var checkedWord = string.Concat(variation);
            _logger.LogInformation("Checking string \"{CheckedString}\"", checkedWord);
            if (!CheckHashMatch(CalculateHash(variation))) continue;
            _logger.LogInformation("Found matching string \"{MatchedString}\" with hash {ProcessedHash}",
                checkedWord,
                task.Hash);
            SendMatchedString(checkedWord);
        }

        _logger.LogInformation("Finished processing hash: {ProcessedHash}", task.Hash);
        await _managerHttpClient.PatchAsync("/internal/api/manager/hash/crack/request/" + _taskId,
            JsonContent.Create(new CrackWorkerTaskCompletionDto(
                _task.WorkerId,
                Status.Ready)));
    }

    private bool CheckHashMatch(string hash) => _task.Hash == hash;

    private static string CalculateHash(IReadOnlyList<char> word)
    {
        var byteArray = word.Select(Convert.ToByte).ToArray();
        var hash = MD5.HashData(byteArray);
        return HashToString(hash);
    }

    private static string HashToString(byte[] bytes) => string.Concat(bytes.Select(b => b.ToString("X2").ToLower()));

    private async void SendMatchedString(string? matchedWord)
        => await _managerHttpClient.PatchAsync("/internal/api/manager/hash/crack/request/" + _taskId,
            JsonContent.Create(new CrackWorkerTaskCompletionDto(
                _task.WorkerId,
                matchedWord != null ? Status.InProgress : Status.Error,
                matchedWord ?? "An error occured")));
}