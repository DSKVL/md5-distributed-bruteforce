using System.Security.Cryptography;
using Combinatorics.Collections;
using HashCrack.Components.Model;
using Microsoft.Extensions.Logging;

namespace HashCrack.Components.Service;

public class WorkerService
{
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<WorkerJobResult> Crack(WorkerJob job)
    {
        _logger.LogInformation(
            "Started processing hash {ProcessedHash} with task offset {TaskOffset} and count {TaskCount}",
            job.Hash, job.Offset, job.SendCount);

        foreach (var word in RequestedWordsEnumerator(job))
        {
            _logger.LogTrace("Checking string \"{CheckedString}\"",
                word);

            var hash = CalculateHash(word);
            if (hash != job.Hash) continue;

            _logger.LogInformation("Found matching string \"{MatchedString}\" with hash {ProcessedHash}",
                word, job.Hash);

            yield return new WorkerJobResult(job.TaskId, job.JobId, Status.InProgress, word);
        }

        _logger.LogInformation("Finished processing hash: {ProcessedHash}", job.Hash);

        yield return new WorkerJobResult(job.TaskId, job.JobId, Status.Ready);
    }

    private static IEnumerable<string> RequestedWordsEnumerator(WorkerJob job) => Enumerable
        .Range(1, (int)job.MaxLength)
        .Select(lowerIndex => new Variations<char>(job.Alphabet, lowerIndex, GenerateOption.WithRepetition))
        .Aggregate(Enumerable.Empty<IReadOnlyList<char>>(),
            (accumulatorEnumerable, enumerable) => accumulatorEnumerable.Concat(enumerable))
        .Skip((int)job.Offset)
        .Take((int)job.SendCount)
        .Select(variation => string.Concat(variation));

    private static string CalculateHash(string word)
    {
        var byteArray = word.Select(Convert.ToByte).ToArray();
        var hash = MD5.HashData(byteArray);
        return HashToString(hash);
    }

    private static string HashToString(byte[] bytes) => string.Concat(bytes.Select(b => b.ToString("X2").ToLower()));
}