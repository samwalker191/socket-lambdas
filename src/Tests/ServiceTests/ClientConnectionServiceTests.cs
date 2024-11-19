using Domain;
using FluentAssertions;
using MongoDB.Driver;
using Services;
using Services.Interfaces;
using Xunit;

namespace Tests.ServiceTests;

public class ClientConnectionServiceTests
{
    private readonly IClientConnectionService _clientConnectionService;
    private readonly IMongoCollection<Client> _clientCollection;

    public ClientConnectionServiceTests()
    {
        // set db string to be local test db
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "mongodb://localhost:27017");
        _clientConnectionService = new ClientConnectionService();
        
        // for assertions 
        var mongoClient = new MongoClient("mongodb://localhost:27017");
        _clientCollection = mongoClient.GetDatabase("rebuild-test").GetCollection<Client>("clients");
    }

    [Fact]
    public async Task ShouldBeAbleToCreateAClientConnectionAndReturnTrue()
    {
        // Arrange
        var connectionId = "test-connection-id";
        
        // Act
        var result = await _clientConnectionService.CreateClientConnection(connectionId);
        var clientConnection = _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefault();
        
        // Assert
        clientConnection.ConnectionId.Should().Be(connectionId);
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task ShouldNotCreateAClientConnectionWithEmptyInput()
    {
        // Arrange
        var badConnectionId1 = "";
        var badConnectionId2 = "      ";
        
        
        // Act
        var badResult1 = await _clientConnectionService.CreateClientConnection(badConnectionId1);
        var badResult2 = await _clientConnectionService.CreateClientConnection(badConnectionId1);
        var clientConnection1 = _clientCollection.Find(x => x.ConnectionId == badConnectionId1).FirstOrDefault();
        var clientConnection2 = _clientCollection.Find(x => x.ConnectionId == badConnectionId2).FirstOrDefault();
        
        // Assert
        clientConnection1.Should().BeNull();
        clientConnection2.Should().BeNull();
        badResult1.Should().BeFalse();
        badResult2.Should().BeFalse();
    }
}