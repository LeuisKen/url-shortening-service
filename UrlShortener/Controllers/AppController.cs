using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using NanoidDotNet;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("app/api", Name = "UrlShortenerManagement")]
public class AppController(
    IDynamoDBContext dbContext,
    ILogger<AppController> logger
) : ControllerBase
{
    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly ILogger<AppController> _logger = logger;

    private async Task<string> GenerateAlias()
    {
        string aliasTrial = Nanoid.Generate(size: 6, alphabet: "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        var existingUrl = await _dbContext.LoadAsync<Url>(aliasTrial);
        if (existingUrl != null)
        {
            return await GenerateAlias();
        }
        return aliasTrial;
    }

    [HttpPost("createUrl", Name = "CreateUrl")]
    public async Task<ActionResult<Url>> PostUrl(UrlDTO urlDTO)
    {
        try {
            var hasCustomAlias = !string.IsNullOrEmpty(urlDTO.CustomAlias);

            if (hasCustomAlias)
            {
                if (urlDTO.CustomAlias!.Length < 4)
                {
                    return BadRequest(new {
                        status = 400,
                        msg = "The custom alias must be at least 6 characters long."
                    });
                }

                var existingUrl = await _dbContext.LoadAsync<Url>(urlDTO.CustomAlias);
                if (existingUrl != null)
                {
                    return BadRequest(new {
                        status = 400,
                        msg = "The custom alias conflict with existing short links."
                    });
                }
            }
            else {
                urlDTO.CustomAlias = await GenerateAlias();
            }

            var hasExpireDate = urlDTO.ExpireDate != null;
            var expireDateMax = DateTimeOffset.Now.AddYears(5).ToUnixTimeMilliseconds();

            if (hasExpireDate)
            {
                var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var expireDate = urlDTO.ExpireDate!.Value;
                if (expireDate < now)
                {
                    return BadRequest(
                        new {
                            status = 400,
                            msg = "The expire date must be greater than current date."
                        }
                    );
                }

                if (expireDate > expireDateMax)
                {
                    return BadRequest(
                        new {
                            status = 400,
                            msg = "The expire date must be less than 5 years."
                        }
                    );
                }
            }
            else {
                // using the max expire date as default
                urlDTO.ExpireDate = expireDateMax;
            }

            var todoItem = new Url
            {
                OriginalUrl = urlDTO.OriginalUrl,
                Alias = urlDTO.CustomAlias!,
                CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                ExpireDate = urlDTO.ExpireDate!.Value,
            };

            await _dbContext.SaveAsync(todoItem);

            return Ok(new {
                status = 0,
                msg = "",
                data = new {
                    url = $"{Request.Scheme}://{Request.Host}/{todoItem.Alias}"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating short link");
            return StatusCode(500, new
            {
                status = 500,
                msg = "Please contact the administrator for help."
            });
        }

    }
}
