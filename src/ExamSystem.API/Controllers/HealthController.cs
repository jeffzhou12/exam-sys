using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult Get()
        => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
