using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Domain;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Publish;
using Services.Interfaces;
using Xunit;

namespace Tests.FunctionTests;

public class PublishFunctionTests
{
    public PublishFunctionTests()
    {
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "mongodb://localhost:27017");
    }
    
    [Fact]
    public async Task ShouldReturnBadRequestIfNoTopicNorMessageInRequestBody()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequestWithNoTopic = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithPublishBodyButNoTopic);
        var apiGatewayProxyRequestWithNoMessage = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithPublishBodyButNoMessage);
        var context = new TestLambdaContext();
        
        // Act
        var noTopicResult = await functionToTest.FunctionHandler(apiGatewayProxyRequestWithNoTopic!, context);
        var noMessageResult = await functionToTest.FunctionHandler(apiGatewayProxyRequestWithNoMessage!, context);
         
        // Assert
        noTopicResult.StatusCode.Should().Be(400);
        noTopicResult.Body.Should().Be("Unable to process message");
        noMessageResult.StatusCode.Should().Be(400);
        noMessageResult.Body.Should().Be("Unable to process message");
    }
    
    [Fact]
    public async Task ShouldCatchErrorsInRequestBodyDeserialization()
    {
        // Arrange
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithErrorCausingPublishBody);
        var context = new TestLambdaContext();
        
        // Act
        var failedResult = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
         
        // Assert
        failedResult.StatusCode.Should().Be(500);
        failedResult.Body.Should().Contain("Failed to publish messages:");
    }
    
    [Fact]
    public async Task ShouldReturnSuccessResponseWithValidRequest()
    {
        // Arrange
        var client = new Client("banana")
        {
            Subscriptions = new List<string> { "science " }
        };
        var mockClientConnectionService = new Mock<IClientConnectionService>();
        mockClientConnectionService.Setup(x => x.GetClientsByTopic(It.IsAny<string>()))
            .ReturnsAsync(new List<Client> { client });
        
        var functionToTest = new Function(mockClientConnectionService.Object);
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestWithValidPublishBody);
        var context = new TestLambdaContext();
        
        // Act
        var result = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        
        // Assert
        result.StatusCode.Should().Be(200);
        result.Body.Should().Contain("Data send to connections for topic:");
    }
}