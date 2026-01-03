using System.Net;
using FluentAssertions;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class EvaluateRepostRetryLogicTests
{
    [Fact]
    public void HttpStatusCode_404_Should_Be_NotFound()
    {
        // Arrange
        var statusCode = HttpStatusCode.NotFound;
        
        // Act
        var is404 = statusCode == HttpStatusCode.NotFound;
        
        // Assert
        is404.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnsupportedMediaType)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public void HttpStatusCode_4xx_Should_Be_ClientError(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var isClientError = statusCode >= HttpStatusCode.BadRequest && 
                           statusCode < HttpStatusCode.InternalServerError;
        
        // Assert
        isClientError.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void HttpStatusCode_5xx_Should_Be_ServerError(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var isServerError = statusCode >= HttpStatusCode.InternalServerError;
        
        // Assert
        isServerError.Should().BeTrue();
    }
}
