using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain;
using MongoDB.Driver;
using Services;
using Services.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OnDisconnect
{
    public class Function
    {
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
            try
            {
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                
                var result = await _clientConnectionService.DeleteClientConnectionByConnectionId(connectionId);
                
                if (!result) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to disconnect"
                    };

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "Disconnected"
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error disconnecting: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to disconnect: {e.Message}" 
                };
            }
        }
    }
}