using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Services;
using Services.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OnConnect
{
    public class Function
    {
        private static readonly string? ApiKey = Environment.GetEnvironmentVariable("API_KEY");
        private static IClientConnectionService _clientConnectionService = null!;

        // Lambda invokes a parameterless constructor function when warming up
        // Structuring it this way allows for mocks to be used for testing more easily
        public Function(): this(null)
        {
        }

        public Function(IClientConnectionService? clientConnectionService)
        {
            _clientConnectionService = clientConnectionService ?? new ClientConnectionService();
        }
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (!request.Headers.TryGetValue("x-api-key", out var apiKey) || ApiKey != apiKey)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 401,
                    Body = "Unauthorized" 
                };
            }

            try
            {
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                
                var result = await _clientConnectionService.CreateClientConnection(connectionId);
                
                if (!result) return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Failed to connect" 
                };

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "Connected"
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error connecting: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to connect: {e.Message}" 
                };
            }
        }
    }
}