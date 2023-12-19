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
    IConfiguration configuration,
    ILogger<AppController> logger
) : ControllerBase
{

    // default values
    private const string REDIRECT_CACHE_PREFIX = "redirect:";
    private const int REDIRECT_CACHE_EXPIRES_BY_SECONDS = 259200;

    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly IDistributedCache _cache = cache;
    private readonly IConfigurationSection _configurationSection = configuration.GetSection("AppConfig");
    private readonly ILogger<AppController> _logger = logger;

    [HttpGet("{alias}", Name = "Redirect")]
    public async Task<IActionResult> Get(string alias)
    {
        try {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Url? url = null;

            string redirectCachePrefix = _configurationSection.GetValue("RedirectCachePrefix", REDIRECT_CACHE_PREFIX)!;
            string cacheKey = redirectCachePrefix + alias;

            var urlInCache = await _cache.GetStringAsync(cacheKey);
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
                int redirectCacheExpiresBySeconds = _configurationSection.GetValue("RedirectCacheExpiresBySeconds", REDIRECT_CACHE_EXPIRES_BY_SECONDS);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = new TimeSpan(0, 0, redirectCacheExpiresBySeconds)
                };
                await _cache.SetStringAsync(
                    alias,
                    JsonSerializer.Serialize(url),
                    options
                );
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
