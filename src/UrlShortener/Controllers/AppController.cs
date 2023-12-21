using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using NanoidDotNet;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("app/api", Name = "UrlShortenerManagement")]
public class AppController(
    IDynamoDBContext dbContext,
    IConfiguration configuration,
    ILogger<AppController> logger
) : ControllerBase
{

    // default values
    private const int DEFAULT_NANOID_SIZE = 6;
    private const string DEFAULT_NANOID_ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuv";
    private const int CUSTOM_ALIAS_LENGTH_MIN = 4;
    private const int URL_EXPIRE_DATE_MAX_BY_SECONDS = 157680000;

    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly IConfigurationSection _configurationSection = configuration.GetSection("AppConfig");
    private readonly ILogger<AppController> _logger = logger;

    private async Task<string> GenerateAlias()
    {
        int nanoidSize = _configurationSection.GetValue("NanoidSize", DEFAULT_NANOID_SIZE);
        string nanoidAlphabet = _configurationSection.GetValue("NanoidAlphabet", DEFAULT_NANOID_ALPHABET)!;
        string aliasTrial = Nanoid.Generate(size: nanoidSize, alphabet: nanoidAlphabet);
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
            int customAliasLengthMin = _configurationSection.GetValue("CustomAliasLengthMin", CUSTOM_ALIAS_LENGTH_MIN);
            var hasCustomAlias = !string.IsNullOrEmpty(urlDTO.CustomAlias);

            if (hasCustomAlias)
            {
                if (urlDTO.CustomAlias!.Length < customAliasLengthMin)
                {
                    return BadRequest(new {
                        status = 400,
                        msg = $"The custom alias must be at least {customAliasLengthMin} characters long."
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

            int urlExpireDateMaxBySeconds = _configurationSection.GetValue("UrlExpireDateMaxBySeconds", URL_EXPIRE_DATE_MAX_BY_SECONDS);
            var hasExpireDate = urlDTO.ExpireDate != null;
            var expireDateMax = DateTimeOffset.Now.AddSeconds(urlExpireDateMaxBySeconds).ToUnixTimeMilliseconds();

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
                            msg = "The expire date is larger than the maximum allowed."
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
