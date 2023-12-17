using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.Controllers;

[ApiController]
[Route("app/api", Name = "UrlShortenerManagement")]
public class AppController : ControllerBase
{
    private readonly ILogger<AppController> _logger;

    public AppController(ILogger<AppController> logger)
    {
        _logger = logger;
    }

    [HttpPost("createUrl", Name = "CreateUrl")]
    public IEnumerable<int> PostUrl()
    {
        return new List<int>();
    }
}
