using Domain;
using MongoDB.Driver;
using Services.Interfaces;

namespace Services;

public class ClientConnectionService : IClientConnectionService
{
    private readonly IMongoCollection<Client> _clientsCollection;

    public ClientConnectionService()
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString)) throw new ApplicationException("DB_CONNECTION_STRING environment variable is not defined");
        var mongoClient = new MongoClient(connectionString);
        _clientsCollection = mongoClient.GetDatabase("rebuild-test").GetCollection<Client>("clients");
    }
    
    public async Task<bool> CreateClientConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) return false;
        
        var client = new Client(connectionId);
        var result = await _clientsCollection.ReplaceOneAsync(x => x.ConnectionId == connectionId, 
            client, new ReplaceOptions { IsUpsert = true });

        return result.IsAcknowledged;
    }
}
