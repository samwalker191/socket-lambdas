using MongoDB.Bson;

namespace Domain;

public class Client(string connectionId, bool connected)
{
    public ObjectId Id { get; set; }
    
    public string ConnectionId { get; set; } = connectionId;

    public bool Connected { get; set; } = connected;
    
    public List<string> Subscriptions { get; set; } = new();
}
