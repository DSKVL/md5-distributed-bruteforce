using HashCrack.Manager.DTO;
using Manager.Service;
using Microsoft.AspNetCore.Mvc;

namespace HashCrack.Manager.Controllers;

[ApiController]
[Route("/api/internal/task")]
public class TaskController : ControllerBase
{
    private readonly ILogger<TaskController> _logger;
    private readonly WorkerService _workerService;

    public TaskController(ILogger<TaskController> logger,
        WorkerService workerService,
        IConfiguration configuration)
    {
        _logger = logger;
        _workerService = workerService;
    }

    [HttpPatch(Name = "PostMD5Hash")]
    public async Task Get(
        [FromQuery] Guid taskId,
        [FromBody] HashCrackRequestDto request)
        => _workerService.CreateTask(request.hash, request.maxLength);
}