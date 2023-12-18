using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using NanoidDotNet;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("app/api", Name = "UrlShortenerManagement")]
public class AppController(
    IDynamoDBContext context,
    ILogger<AppController> logger
) : ControllerBase
{
    private readonly IDynamoDBContext _context = context;
    private readonly ILogger<AppController> _logger = logger;

    private async Task<string> GenerateAlias()
    {
        string aliasTrial = Nanoid.Generate(size: 6, alphabet: "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        var existingUrl = await _context.LoadAsync<Url>(aliasTrial);
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
                var existingUrl = await _context.LoadAsync<Url>(urlDTO.CustomAlias);
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

            var hasExpireDate = !string.IsNullOrEmpty(urlDTO.ExpireDate);

            if (hasExpireDate)
            {
                var expireDate = DateTime.Parse(urlDTO.ExpireDate!);
                if (expireDate < DateTime.Now)
                {
                    return BadRequest(
                        new {
                            status = 400,
                            msg = "The expire date must be greater than current date."
                        }
                    );
                }
            }
            else {
                urlDTO.ExpireDate = DateTime.Now.AddYears(5).ToString();
            }

            var todoItem = new Url
            {
                OriginalUrl = urlDTO.OriginalUrl,
                Alias = urlDTO.CustomAlias!,
                CreateTime = DateTime.Now.ToString(),
                ExpireDate = urlDTO.ExpireDate!,
            };

            await _context.SaveAsync(todoItem);

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
            return BadRequest(new {
                status = 500,
                msg = "Please contact the administrator for help."
            });
        }

    }
}
