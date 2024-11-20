using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Services.Interfaces;
using Subscribe;
using Xunit;

namespace Tests.FunctionTests;

public class SubscribeFunctionTests
{
    public SubscribeFunctionTests()
    {
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "mongodb://localhost:27017");
    }
    
    [Fact]
    public async Task ShouldReturnBadRequestIfNoTopicInRequestBody()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithSubscribeBodyButNoTopic);
        var context = new TestLambdaContext();
        
        // Act
         var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
         
        // Assert
        failedResult.StatusCode.Should().Be(400);
        failedResult.Body.Should().Be("Unable to process message");
    }
    
    [Fact]
    public async Task ShouldReturnBadRequestIfFailedToSubscribeToTopic()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.SubscribeToTopicByConnectionId("banana","science")).ReturnsAsync(false);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithValidSubscribeBody);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
         
        // Assert
        failedResult.StatusCode.Should().Be(400);
        failedResult.Body.Should().Be("Unable to subscribe to topic");
    }
    
    [Fact]
    public async Task ShouldCatchErrorsInRequestBodyDeserialization()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithErrorCausingSubscribeBody);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
         
        // Assert
        failedResult.StatusCode.Should().Be(500);
        failedResult.Body.Should().Contain("Failed to subscribe to topic:");
    }
    
    [Fact]
    public async Task ShouldCatchErrorsOnAttemptToDeleteClientConnectionAndReturnInternalServerErrorResponse()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.SubscribeToTopicByConnectionId("banana", "science")).Throws(new Exception());
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithValidSubscribeBody);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        
        // Assert
        failedResult.StatusCode.Should().Be(500);
        failedResult.Body.Should().Contain("Failed to subscribe to topic:");
    }

    [Fact]
    public async Task ShouldReturnSuccessResponseIfSuccessfullySubscribedToTopic()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.SubscribeToTopicByConnectionId("banana", "science")).ReturnsAsync(true);
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithValidSubscribeBody);
        var context = new TestLambdaContext();
        
        // Act
        var result = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        
        // Assert
        result.StatusCode.Should().Be(200);
        result.Body.Should().Contain("Subscribed to topic:");
    }
}