using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain;
using MongoDB.Driver;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Subscribe
{
    public class Function
    {
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
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");
                var message = JsonConvert.DeserializeObject<SubscribeMessage>(request.Body);
                if (message?.Topic == null) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to process message"
                    };
                
                context.Logger.LogLine($"Message: {message}");
                
                var client = await clientCollection.Find(c => c.ConnectionId == connectionId).FirstOrDefaultAsync();
                if (client == null) return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Unable to subscribe to topic"
                    };
                
                var currentSubscriptions = client.Subscriptions;

                if (!currentSubscriptions.Contains(message.Topic))
                {
                    currentSubscriptions.Add(message.Topic.ToLower());
                    var update = Builders<Client>.Update.Set(c => c.Subscriptions, currentSubscriptions);
                    await clientCollection.FindOneAndUpdateAsync(x => x.ConnectionId == connectionId, update);
                }
                
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
                    Body = $"Failed to connect: {e.Message}" 
                };
            }
        }
    }
}