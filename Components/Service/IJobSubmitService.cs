using HashCrack.Components.Model;

namespace HashCrack.Components.Service;

public interface IJobSubmitService
{
    public Task<CrackTask> CreateAndSubmitJobs(string targetHash, uint maxSourceLength);
}