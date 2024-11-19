using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain;
using MongoDB.Driver;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OnDisconnect
{
    public class Function
    {
        private static MongoClient? _mongoClient;

        private static MongoClient CreateMongoClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString)) throw new ApplicationException("DB_CONNECTION_STRING environment variable is not defined");
            return new MongoClient(connectionString);
        }

        static Function()
        {
            _mongoClient = CreateMongoClient();
        }
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (_mongoClient == null)
            {
                context.Logger.LogError("Database client is not initialized");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Failed to disconnect"
                };
            }
            
            var database = _mongoClient.GetDatabase("rebuild-test");
            var clientCollection = database.GetCollection<Client>("clients");
            try
            {
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                
                var result = await clientCollection.DeleteOneAsync(x => x.ConnectionId == connectionId);
                
                if (!result.IsAcknowledged) return new APIGatewayProxyResponse
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