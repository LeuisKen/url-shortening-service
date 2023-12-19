using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("", Name = "Redirect")]
public class RedirectController(
    IDynamoDBContext dbContext,
    ILogger<AppController> logger
) : ControllerBase
{

    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly ILogger<AppController> _logger = logger;

    [HttpGet("{alias}", Name = "Redirect")]
    public async Task<IActionResult> Get(string alias)
    {
        try {
            var url = await _dbContext.LoadAsync<Url>(alias);
            if (url == null)
            {
                return NotFound();
            }
            return Redirect(url.OriginalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while redirecting");
            return StatusCode(500);
        }
    }

}
