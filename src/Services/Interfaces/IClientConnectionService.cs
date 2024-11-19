using Domain;

namespace Services.Interfaces;

public interface IClientConnectionService
{
    Task<bool> CreateClientConnection(string connectionId);
    
    Task<bool> DeleteClientConnectionByConnectionId(string connectionId);
    
    Task<bool> SubscribeToTopicByConnectionId(string connectionId, string topic);
    
    Task<List<Client>> GetClientsByTopic(string topic);
}