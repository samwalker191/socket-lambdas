using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using OnConnect;
using Services.Interfaces;
using Xunit;

namespace Tests.FunctionTests;

public class OnConnectFunctionTests
{
    public OnConnectFunctionTests()
    {
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("API_KEY", "apikey");
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedIfInvalidApiKey()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequestWithBadApiKey = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithBadApiKey);
        var apiGatewayProxyRequestWithoutApiKey = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithoutApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var badKeyResult = await functionToTest.FunctionHandler(apiGatewayProxyRequestWithBadApiKey!, context);
        var noKeyResult = await functionToTest.FunctionHandler(apiGatewayProxyRequestWithoutApiKey!, context);
        // Assert
        badKeyResult.StatusCode.Should().Be(401);
        badKeyResult.Body.Should().Be("Unauthorized");
        
        noKeyResult.StatusCode.Should().Be(401);
        noKeyResult.Body.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task ShouldReturnBadRequestIfFailedToCreateClientConnection()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.CreateClientConnection("banana")).ReturnsAsync(false);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        // Assert
        failedResult.StatusCode.Should().Be(400);
        failedResult.Body.Should().Be("Failed to connect");
    }
    
    [Fact]
    public async Task ShouldCatchErrorsOnAttemptToCreateClientConnectionAndReturnInternalServerErrorResponse()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.CreateClientConnection("banana")).Throws(new Exception());
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        // Assert
        failedResult.StatusCode.Should().Be(500);
        failedResult.Body.Should().Contain("Failed to connect");
    }

    [Fact]
    public async Task ShouldReturnSuccessResponseOnSuccessfulClientConnectionCreation()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.CreateClientConnection("banana")).ReturnsAsync(true);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var result = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        // Assert
        result.StatusCode.Should().Be(200);
        result.Body.Should().Contain("Connected");
    }
}