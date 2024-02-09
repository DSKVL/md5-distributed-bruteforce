using HashCrack.Manager.DTO;
using Manager.Service;
using Microsoft.AspNetCore.Mvc;

namespace HashCrack.Manager.Controllers;

[ApiController]
[Route("/api/hash/status")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly WorkerService _workerService;

    public StatusController(ILogger<StatusController> logger,
        WorkerService workerService)
    {
        _logger = logger;
        _workerService = workerService;
    }

    [HttpGet(Name = "CrackMD5Hash")]
    public async Task<HashCrackResponseDto> Get(HashCrackRequestDto request)
        => new(await _workerService.CreateTask(request.hash, request.maxLength));
}