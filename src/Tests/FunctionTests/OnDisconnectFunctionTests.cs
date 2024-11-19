using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Services.Interfaces;
using Xunit;
using OnDisconnect;

namespace Tests.FunctionTests;

public class OnDisconnectFunctionTests
{
    public OnDisconnectFunctionTests()
    {
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "mongodb://localhost:27017");
    }
    
    [Fact]
    public async Task ShouldReturnBadRequestIfFailedToDeleteClientConnection()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.DeleteClientConnectionByConnectionId("banana")).ReturnsAsync(false);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        // Assert
        failedResult.StatusCode.Should().Be(400);
        failedResult.Body.Should().Be("Unable to disconnect");
    }
    
    [Fact]
    public async Task ShouldCatchErrorsOnAttemptToDeleteClientConnectionAndReturnInternalServerErrorResponse()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.DeleteClientConnectionByConnectionId("banana")).Throws(new Exception());
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        
        // Assert
        failedResult.StatusCode.Should().Be(500);
        failedResult.Body.Should().Contain("Failed to disconnect:");
    }
    
    [Fact]
    public async Task ShouldReturnSuccessResponseOnSuccessfulClientConnectionDeletion()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.DeleteClientConnectionByConnectionId("banana")).ReturnsAsync(true);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithApiKey);
        var context = new TestLambdaContext();
        
        // Act
        var result = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        // Assert
        result.StatusCode.Should().Be(200);
        result.Body.Should().Contain("Disconnected");
    }
}