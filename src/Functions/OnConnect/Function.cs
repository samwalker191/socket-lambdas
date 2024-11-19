using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Domain;
using MongoDB.Driver;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OnConnect
{
    public class Function
    {
        private static readonly string? ApiKey;
        private static readonly MongoClient? MongoClient;

        private static MongoClient CreateMongoClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString)) throw new ApplicationException("DB_CONNECTION_STRING environment variable is not defined");
            return new MongoClient(connectionString);
        }

        static Function()
        {
            MongoClient = CreateMongoClient();
            ApiKey = Environment.GetEnvironmentVariable("API_KEY");
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
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                var client = new Client(connectionId);
                
                var result = await clientCollection.ReplaceOneAsync(x => x.ConnectionId == connectionId, 
                    client, new ReplaceOptions { IsUpsert = true });
                
                if (!result.IsAcknowledged) return new APIGatewayProxyResponse
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