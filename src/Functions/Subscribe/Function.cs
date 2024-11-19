using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain;
using MongoDB.Driver;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Subscribe
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
                    Body = "Failed to connect"
                };
            }
            
            var database = _mongoClient.GetDatabase("rebuild-test");
            var clientCollection = database.GetCollection<Client>("Clients");
            try
            {
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                var client = new Client(connectionId, true);
                
                var result = await clientCollection.ReplaceOneAsync(x => x.ConnectionId == connectionId, 
                    client, new ReplaceOptions { IsUpsert = true });
                
                Console.WriteLine(result);

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