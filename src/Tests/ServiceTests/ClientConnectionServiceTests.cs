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
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        clientConnection.ConnectionId.Should().Be(connectionId);
        result.Should().BeTrue();
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
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
        var clientConnection1 = await _clientCollection.Find(x => x.ConnectionId == badConnectionId1).FirstOrDefaultAsync();
        var clientConnection2 = await _clientCollection.Find(x => x.ConnectionId == badConnectionId2).FirstOrDefaultAsync();
        
        // Assert
        clientConnection1.Should().BeNull();
        clientConnection2.Should().BeNull();
        badResult1.Should().BeFalse();
        badResult2.Should().BeFalse();
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }

    [Fact]
    public async Task ShouldBeAbleToDeleteAClientConnection()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var client = new Client(connectionId);
        await _clientCollection.InsertOneAsync(client);
        
        // Act
        var result = await _clientConnectionService.DeleteClientConnectionByConnectionId(connectionId);
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        clientConnection.Should().BeNull();
        result.Should().BeTrue();
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }
}