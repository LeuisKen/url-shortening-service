using Amazon.DynamoDBv2.DataModel;

using NanoidDotNet;
using UrlShortener.Models;

namespace UrlShortener.Services;

public interface IAliasGenerationService
{
    Task<string> GenerateAlias();
}

public class AliasGenerationService(
    IDynamoDBContext dbContext,
    IConfiguration configuration
) : IAliasGenerationService
{
    private const int DEFAULT_NANOID_SIZE = 6;
    private const string DEFAULT_NANOID_ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuv";

    private readonly IDynamoDBContext _dbContext = dbContext;
    private readonly IConfigurationSection _configurationSection = configuration.GetSection("AppConfig");

    public async Task<string> GenerateAlias()
    {
        int nanoidSize = _configurationSection.GetValue("NanoidSize", DEFAULT_NANOID_SIZE);
        string nanoidAlphabet = _configurationSection.GetValue("NanoidAlphabet", DEFAULT_NANOID_ALPHABET)!;

        while (true)
        {
            string aliasTrial = Nanoid.Generate(size: nanoidSize, alphabet: nanoidAlphabet);
            var existingUrl = await _dbContext.LoadAsync<Url>(aliasTrial);

            if (existingUrl == null)
            {
                return aliasTrial;
            }
        }
    }
}
