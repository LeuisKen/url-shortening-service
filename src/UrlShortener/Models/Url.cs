using Amazon.DynamoDBv2.DataModel;

namespace UrlShortener.Models
{
    [DynamoDBTable("Url")]
    public class Url
    {
        [DynamoDBHashKey("Alias")]
        public required string Alias { get; set; }
        public required string OriginalUrl { get; set; }
        public required long CreateTime { get; set; }
        public required long ExpireDate { get; set; }
    }
}
