using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Default;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Tests.FunctionTests;

public class DefaultFunctionTests
{
    [Fact]
    public async Task ShouldReturnNotFoundResponse()
    {
        // Arrange
        var functionToTest = new Function();
        
        var apiGatewayProxyRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ApiGatewayProxyRequestDefaultBody);
        var context = new TestLambdaContext();
        
        // Act
        var result = await functionToTest.FunctionHandler(apiGatewayProxyRequest!, context);
        
        // Assert
        result.StatusCode.Should().Be(404);
        result.Body.Should().Be("No route found");
    }
}