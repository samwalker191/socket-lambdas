namespace Services.Interfaces;

public interface IClientConnectionService
{
    Task<bool> CreateClientConnection(string connectionId);
}