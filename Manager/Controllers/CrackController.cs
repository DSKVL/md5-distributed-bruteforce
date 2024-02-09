using HashCrack.Manager.DTO;
using Manager.Service;
using Microsoft.AspNetCore.Mvc;

namespace HashCrack.Manager.Controllers;

[ApiController]
[Route("/api/hash/crack")]
public class CrackController : ControllerBase
{
    private readonly WorkerService _workerService;

    public CrackController(WorkerService workerService)
    {
        _workerService = workerService;
    }

    [HttpGet(Name = "CrackMD5Hash")]
    public async Task<HashCrackResponseDto> Get(HashCrackRequestDto request)
        => new(await _workerService.CreateTask(request.hash, request.maxLength));
}