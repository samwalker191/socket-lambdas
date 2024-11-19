using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain;

public class Client(string connectionId)
{
    [BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }
    
    public string ConnectionId { get; set; } = connectionId;
    
    public List<string> Subscriptions { get; set; } = new();
}
