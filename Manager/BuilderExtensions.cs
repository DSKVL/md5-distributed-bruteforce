namespace Manager;

public static class WorkerClientExtensions
{
    public static void AddWorkersClients(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var clientHostnamePrefix = configuration["WORKER_HOSTNAME_PREFIX"];
        var workerCount = uint.Parse(configuration["WORKER_COUNT"] ?? "1");

        foreach (var idx in Enumerable.Range(1, (int)workerCount))
        {
            Console.WriteLine($"http://{clientHostnamePrefix + idx}");
            builder.Services.AddHttpClient(clientHostnamePrefix + idx,
                c => { c.BaseAddress = new($"http://{clientHostnamePrefix + idx}:8080"); });
        }
    }

    public static IList<HttpClient> GetWorkersClient<T>(this IHttpClientFactory factory,
        IConfiguration configuration,
        ILogger<T> logger)
    {
        var clientHostnamePrefix = configuration["WORKER_HOSTNAME_PREFIX"];
        var workerCount = uint.Parse(configuration["WORKER_COUNT"] ?? "1");
        logger.LogInformation("Worker prefix is {WorkerHostnamePrefix}, worker count is {WorkerCount}",
            clientHostnamePrefix, workerCount);
        return Enumerable.Range(1, (int)workerCount)
            .Select(idx => factory.CreateClient(clientHostnamePrefix + idx))
            .ToList();
    }
}