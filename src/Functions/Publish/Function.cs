using System.Net;
using System.Text;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Domain;
using MongoDB.Driver;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Publish
{
    public class Function
    {
        private static readonly MongoClient? MongoClient;

        private static MongoClient CreateMongoClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString)) throw new ApplicationException("DB_CONNECTION_STRING environment variable is not defined");
            return new MongoClient(connectionString); //dadwadawd
        }

        static Function()
        {
            MongoClient = CreateMongoClient();
        }
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (MongoClient == null)
            {
                context.Logger.LogError("Database client is not initialized");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Failed to connect"
                };
            }
            
            var database = MongoClient.GetDatabase("rebuild-test");
            var clientCollection = database.GetCollection<Client>("clients");
            
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

                var filter = Builders<Client>.Filter.AnyEq(x => x.Subscriptions, message.Topic.ToLower());
                var clients = await clientCollection.Find(filter).ToListAsync() ?? new List<Client>();
                context.Logger.LogLine($"Found {clients.Count} clients");
                
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(message.Message));
                var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
                {
                    ServiceURL = endpoint
                });

                foreach (var client in clients)
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
                            await clientCollection.FindOneAndDeleteAsync(x => x.ConnectionId == client.ConnectionId);
                        }
                        else
                        {
                            context.Logger.LogLine($"Error posting message to {client.ConnectionId}: {e.Message}");
                            context.Logger.LogLine(e.StackTrace); 
                        }
                    }
                }
                
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = $"Data send to connections for topic: {message.Topic}"
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