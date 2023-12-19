using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("", Name = "Redirect")]
public class RedirectController(
    IDynamoDBContext dbContext,
    IDistributedCache cache,
    ILogger<AppController> logger
) : ControllerBase
{

    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<AppController> _logger = logger;

    [HttpGet("{alias}", Name = "Redirect")]
    public async Task<IActionResult> Get(string alias)
    {
        try {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Url? url = null;
            var urlInCache = await _cache.GetStringAsync(alias);
            if (urlInCache != null)
            {
                url = JsonSerializer.Deserialize<Url>(urlInCache)!;
            }
            else {
                url = await _dbContext.LoadAsync<Url>(alias);
                if (url == null)
                {
                    return NotFound();
                }
                await _cache.SetStringAsync(alias, JsonSerializer.Serialize(url));
            }
            return url.ExpireDate < now
                ? BadRequest(
                    new {
                        status = 400,
                        msg = "The short link has expired."
                    }
                )
                : Redirect(url.OriginalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while redirecting");
            return StatusCode(500);
        }
    }

}
