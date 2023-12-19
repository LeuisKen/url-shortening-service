namespace UrlShortener.Models
{
    public class UrlDTO
    {
        public required string OriginalUrl { get; set; }
        public string? CustomAlias { get; set; }
        public string? ExpireDate { get; set; }
    }
}
