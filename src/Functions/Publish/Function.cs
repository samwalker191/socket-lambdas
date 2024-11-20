using System.Net;
using System.Text;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Domain;
using Newtonsoft.Json;
using Services;
using Services.Interfaces;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Publish
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
                var domainName = request.RequestContext.DomainName;
                var stage = request.RequestContext.Stage;
                var endpoint = $"https://{domainName}/{stage}";
                context.Logger.LogLine($"API Gateway management endpoint: {endpoint}");
                context.Logger.LogLine($"Body: {request.Body}");
                
                var message = JsonConvert.DeserializeObject<PublishMessage>(request.Body);
                if (message?.Topic == null || message.Message == null) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to process message"
                    };
                
                var clients = await _clientConnectionService.GetClientsByTopic(message.Topic.ToLower());
                context.Logger.LogLine($"Found {clients.Count} clients");
                
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(message.Message));
                var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
                {
                    ServiceURL = endpoint
                });

                foreach (var client in clients)
                {
                    await PublishMessageToClient(context, client, stream, apiClient);
                }
                
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = $"Data send to connections for topic: {message.Topic}"
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error publishing messages: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to publish messages: {e.Message}" 
                };
            }
        }

        private static async Task PublishMessageToClient(ILambdaContext context, Client client, MemoryStream stream,
            AmazonApiGatewayManagementApiClient apiClient)
        {
            var postConnectionRequest = new PostToConnectionRequest
            {
                ConnectionId = client.ConnectionId,
                Data = stream
            };

            try
            {
                context.Logger.LogLine($"Post to connection: {client.ConnectionId}");
                stream.Position = 0;
                await apiClient.PostToConnectionAsync(postConnectionRequest);
            }
            catch (AmazonServiceException e)
            {
                if (e.StatusCode == HttpStatusCode.Gone)
                {
                    context.Logger.LogLine($"Deleting gone connection: {client.ConnectionId}");
                    await _clientConnectionService.DeleteClientConnectionByConnectionId(client.ConnectionId);
                }
                else
                {
                    context.Logger.LogLine($"Error posting message to {client.ConnectionId}: {e.Message}");
                    context.Logger.LogLine(e.StackTrace); 
                }
            }
        }
    }
}