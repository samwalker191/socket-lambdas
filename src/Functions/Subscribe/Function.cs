using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain;
using Newtonsoft.Json;
using Services;
using Services.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Subscribe
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
                
                var message = JsonConvert.DeserializeObject<SubscribeMessage>(request.Body);
                if (message?.Topic == null) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to process message"
                    };
                
                var result = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, message.Topic);
                if (!result) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to subscribe to topic"
                    };
                
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = $"Subscribed to topic: {message.Topic}"
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error connecting: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to subscribe to topic: {e.Message}" 
                };
            }
        }
    }
}