using Manager.DTO;
using Manager.Service;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Controllers;

[ApiController]
[Route("/api/hash/crack")]
public class CrackController : ControllerBase
{
    private readonly ILogger<CrackController> _logger;
    private readonly WorkerService _workerService;

    public CrackController(ILogger<CrackController> logger,
        WorkerService workerService,
        IConfiguration configuration)
    {
        _logger = logger;
        _workerService = workerService;
    }

    [HttpGet(Name = "CrackMD5Hash")]
    public async Task<HashCrackResponseDto> Get(HashCrackRequestDto request)
        => new HashCrackResponseDto(await _workerService.CreateTask(request.hash, request.maxLength));
}