using Xunit;
using Moq;
using UrlShortener.Controllers;
using UrlShortener.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace UrlShortener.Tests.Controllers;

public class AppControllerTests
{
    private readonly Mock<IDynamoDBContext> _mockDbContext;
    private readonly AppController _controller;

    public AppControllerTests()
    {
        // Arrange

        // Mocking DynamoDBContext
        var mockDbContext = new Mock<IDynamoDBContext>();
        var mockLogger = new Mock<ILogger<AppController>>();

        // Mocking HttpContext
        var httpContextMock = new Mock<HttpContext>();
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Scheme).Returns("https");
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        httpContextMock.SetupGet(c => c.Request).Returns(requestMock.Object);

        var controller = new AppController(mockDbContext.Object, mockLogger.Object)
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
    public async Task PostUrl_WithValidData_ReturnsOk()
    {
        // Arrange
        var urlDTO = new UrlDTO { OriginalUrl = "http://example.com" };
        _mockDbContext.Setup(
            x => x.LoadAsync<Url?>(It.IsAny<string>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync((Url?)null);
        _mockDbContext.Setup(
            x => x.SaveAsync(It.IsAny<Url>(), It.IsAny<CancellationToken>())
        ).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PostUrl(urlDTO);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task PostUrl_WithExpireDate_ReturnsBadRequestWhenExpireDateIsInThePast()
    {
        // Arrange
        var urlDTO = new UrlDTO
        {
            OriginalUrl = "http://example.com",
            ExpireDate = DateTime.Now.AddDays(-1).ToString()
        };

        // Act
        var result = await _controller.PostUrl(urlDTO);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task PostUrl_WithCustomAlias_ReturnsBadRequestWhenAliasExists()
    {
        // Arrange
        var urlDTO = new UrlDTO
        {
            OriginalUrl = "http://example.com",
            CustomAlias = "customAlias"
        };
        _mockDbContext.Setup(
            x => x.LoadAsync<Url>(urlDTO.CustomAlias, It.IsAny<CancellationToken>())
        ).ReturnsAsync(new Url
        {
            Alias = urlDTO.CustomAlias,
            OriginalUrl = "http://example.com",
            CreateTime = DateTime.Now.ToString(),
            ExpireDate = DateTime.Now.AddYears(5).ToString()
        });

        // Act
        var result = await _controller.PostUrl(urlDTO);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

}
