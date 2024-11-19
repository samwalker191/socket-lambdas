﻿using Domain;
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
        var newClientConnection = new Client(connectionId);
        await _clientCollection.InsertOneAsync(newClientConnection);
        
        // Act
        var result = await _clientConnectionService.DeleteClientConnectionByConnectionId(connectionId);
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        clientConnection.Should().BeNull();
        result.Should().BeTrue();
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }

    [Fact]
    public async Task ShouldNotSubscribeToAnEmptyTopic()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var newClientConnection = new Client(connectionId);
        await _clientCollection.InsertOneAsync(newClientConnection);
        var emptyTopic = "";
        
        // Act
        var result = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, emptyTopic);
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        result.Should().BeFalse();
        clientConnection.Subscriptions.Should().NotContain(emptyTopic);
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }
    
    [Fact]
    public async Task ShouldNotDuplicateSubscriptions()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var topic ="test-subscription";
        var newClientConnection = new Client(connectionId)
        {
            Subscriptions = new List<string> { topic }
        };
        await _clientCollection.InsertOneAsync(newClientConnection);
        
        // Act
        var result = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, topic);
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        result.Should().BeTrue();
        clientConnection.Subscriptions.Should().Contain(topic).And.HaveCount(1);
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }
    
    [Fact]
    public async Task ShouldNotAddSubscriptionToAClientThatDoesNotExist()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var topic ="test-subscription";
        
        // Act
        var result = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, topic);
        
        // Assert
        result.Should().BeFalse();
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }

    [Fact]
    public async Task ShouldBeAbleToAddNewSubscriptionsWithCaseInsensitivity()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var topic ="test-subscription";
        var newTopic = "sports";
        var newCapsTopic = "SCIencE";
        var newClientConnection = new Client(connectionId)
        {
            Subscriptions = new List<string> { topic }
        };
        await _clientCollection.InsertOneAsync(newClientConnection);
        
        // Act
        var sportsResult = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, newTopic);
        var scienceResult = await _clientConnectionService.SubscribeToTopicByConnectionId(connectionId, newCapsTopic);
        var clientConnection = await _clientCollection.Find(x => x.ConnectionId == connectionId).FirstOrDefaultAsync();
        
        // Assert
        sportsResult.Should().BeTrue();
        scienceResult.Should().BeTrue();
        clientConnection.Subscriptions.Should().Contain(topic)
            .And.Contain(newTopic)
            .And.Contain(newCapsTopic.ToLower())
            .And.HaveCount(3);
        
        // Cleanup
        await _clientCollection.DeleteManyAsync(x => true);
    }
}