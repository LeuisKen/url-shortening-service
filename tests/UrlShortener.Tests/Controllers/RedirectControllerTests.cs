using Moq;
using UrlShortener.Controllers;
using UrlShortener.Models;
using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace UrlShortener.Tests.Controllers;

public class RedirectControllerTests
{
    private readonly Mock<IDynamoDBContext> _mockDbContext;
    private readonly RedirectController _controller;

    public RedirectControllerTests()
    {
        // Arrange

        // Mocking DynamoDBContext
        var mockDbContext = new Mock<IDynamoDBContext>();

        // Mocking DistributedCache with MemoryDistributedCache
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        var mockCache = new MemoryDistributedCache(opts);
        var mockLogger = new Mock<ILogger<AppController>>();

        // Mocking HttpContext
        var httpContextMock = new Mock<HttpContext>();
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Scheme).Returns("https");
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        httpContextMock.SetupGet(c => c.Request).Returns(requestMock.Object);

        // Since all the configuration fields have default values, we can just create an empty configuration object.
        var configuration = new ConfigurationBuilder().Build();

        var controller = new RedirectController(
            mockDbContext.Object,
            mockCache,
            configuration,
            mockLogger.Object
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            }
        };

        _mockDbContext = mockDbContext;
        _controller = controller;
    }

    [Fact]
    public async Task Get_WithValidAlias_ReturnsRedirect()
    {
        // Arrange
        var url = new Url
        {
            OriginalUrl = "http://example.com",
            Alias = "alias",
            CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            ExpireDate = DateTimeOffset.Now.AddYears(5).ToUnixTimeMilliseconds()
        };
        _mockDbContext.Setup(
            x => x.LoadAsync<Url>(url.Alias, It.IsAny<CancellationToken>())
        ).ReturnsAsync(url);

        // Act
        var result = await _controller.Get(url.Alias);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(url.OriginalUrl, redirectResult.Url);
    }

    [Fact]
    public async Task Get_WithInvalidAlias_ReturnsNotFound()
    {
        // Arrange
        _mockDbContext.Setup(
            x => x.LoadAsync<Url?>(It.IsAny<string>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync((Url?)null);

        // Act
        var result = await _controller.Get("invalid");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_WithExpiredAlias_ReturnsBadRequest()
    {
        // Arrange
        var url = new Url
        {
            OriginalUrl = "http://example.com",
            Alias = "alias",
            CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            ExpireDate = DateTimeOffset.Now.AddYears(-1).ToUnixTimeMilliseconds()
        };
        _mockDbContext.Setup(
            x => x.LoadAsync<Url>(url.Alias, It.IsAny<CancellationToken>())
        ).ReturnsAsync(url);

        // Act
        var result = await _controller.Get(url.Alias);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
