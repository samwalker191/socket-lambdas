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

    public async Task<bool> DeleteClientConnectionByConnectionId(string connectionId)
    {
        var result = await _clientsCollection.DeleteOneAsync(x => x.ConnectionId == connectionId);
        return result.IsAcknowledged;
    }

    public async Task<bool> SubscribeToTopicByConnectionId(string connectionId, string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return false;
        
        var client = await _clientsCollection.Find(c => c.ConnectionId == connectionId).FirstOrDefaultAsync();
        if (client == null) return false;
        var lowercaseTopic = topic.ToLower();
                
        var currentSubscriptions = client.Subscriptions;
        if (currentSubscriptions.Contains(lowercaseTopic)) return true;

        currentSubscriptions.Add(lowercaseTopic);
        var update = Builders<Client>.Update.Set(c => c.Subscriptions, currentSubscriptions);
        var result = await _clientsCollection.FindOneAndUpdateAsync(x => x.ConnectionId == connectionId, update);
        return result != null;
    }

    public async Task<List<Client>> GetClientsByTopic(string topic)
    {
        var filter = Builders<Client>.Filter.AnyEq(x => x.Subscriptions, topic.ToLower());
        return await _clientsCollection.Find(filter).ToListAsync();
    }
}
