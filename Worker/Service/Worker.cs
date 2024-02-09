using System.Security.Cryptography;
using Combinatorics.Collections;
using Contracts.Contracts.DTO;
using HashCrack.Model;

namespace Worker.Service;

using VariationsEnumerator = IEnumerable<IReadOnlyList<char>>;

public class Worker
{
    private readonly HttpClient _managerHttpClient;
    private readonly WorkerCrackTask _task;

    public Worker(WorkerCrackTask task,
        HttpClient managerHttpClient)
    {
        _task = task;
        _managerHttpClient = managerHttpClient;
    }

    private VariationsEnumerator RequestedStringsEnumerator => Enumerable
        .Range(1, (int)_task.MaxLength)
        .Select(lowerIndex => new Variations<char>(_task.Alphabet, lowerIndex, GenerateOption.WithRepetition))
        .Aggregate(Enumerable.Empty<IReadOnlyList<char>>(),
            (accumulatorEnumerable, enumerable) => accumulatorEnumerable.Concat(enumerable))
        .Skip((int)_task.Offset)
        .Take((int)_task.SendCount);

    public async Task Crack()
    {
        await Parallel.ForEachAsync(RequestedStringsEnumerator, (variation, _) =>
        {
            if (CheckHashMatch(CalculateHash(variation)))
            {
                SendMatchedString(variation.ToString());
            }

            return ValueTask.CompletedTask;
        });
        await _managerHttpClient.SendAsync(new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new WorkerTaskCompletionDto(
                _task.workerId,
                Status.READY))
        });
    }

    private bool CheckHashMatch(string hash) => _task.Hash == hash;

    private string CalculateHash(IReadOnlyList<char> word)
        => MD5.HashData(word.Select(Convert.ToByte).ToArray()).ToString() ?? "";

    private void SendMatchedString(string? matchedWord)
    {
        _managerHttpClient.SendAsync(new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new WorkerTaskCompletionDto(
                _task.workerId,
                matchedWord != null ? Status.IN_PROGRESS : Status.ERROR,
                matchedWord ?? "An error occured"
            ))
        });
    }
}