using System.Threading.Channels;
using SonaChat.GrpcService.Services;
using SonaChat.Protos;

namespace SonaChat.GrpcService.Tests;

public class ChatRoomTests
{
    /// <summary>
    /// Verifies that <see cref="ChatRoom.AddClient(Channel{ChatMessage})"/> returns distinct,
    /// non-null and non-empty identifiers for each newly added client.
    /// </summary>
    /// <remarks>
    /// Arrange: create a new <see cref="ChatRoom"/> and two unbounded channels for <see cref="ChatMessage"/>.
    /// Act: add both channels to the room and capture the returned client IDs.
    /// Assert: each ID must be non-null, non-empty, and the two IDs must not be equal.
    /// Note: GUID-format validation for the returned ID is covered by a separate test.
    /// </remarks>
    [Fact]
    public void AddClient_ReturnsUniqueId()
    {
        // Arrange
        var room = new ChatRoom();
        var channel1 = Channel.CreateUnbounded<ChatMessage>();
        var channel2 = Channel.CreateUnbounded<ChatMessage>();

        // Act
        var id1 = room.AddClient(channel1);
        var id2 = room.AddClient(channel2);

        // Assert
        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEmpty(id1);
        Assert.NotEmpty(id2);
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void AddClient_ReturnsValidGuid()
    {
        // Arrange
        var room = new ChatRoom();
        var channel = Channel.CreateUnbounded<ChatMessage>();

        // Act
        var id = room.AddClient(channel);

        // Assert
        Assert.True(Guid.TryParse(id, out _), "Client ID should be a valid GUID");
    }

    [Fact]
    public async Task BroadcastAsync_WritesToAllActiveClients()
    {
        // Arrange
        var room = new ChatRoom();
        var channel1 = Channel.CreateUnbounded<ChatMessage>();
        var channel2 = Channel.CreateUnbounded<ChatMessage>();

        room.AddClient(channel1);
        room.AddClient(channel2);

        var message = new ChatMessage { User = "alice", Text = "hello world" };

        // Act
        var read1Task = channel1.Reader.ReadAsync().AsTask();
        var read2Task = channel2.Reader.ReadAsync().AsTask();

        await room.BroadcastAsync(message);

        var msg1 = await read1Task;
        var msg2 = await read2Task;

        // Assert
        Assert.Equal("alice", msg1.User);
        Assert.Equal("hello world", msg1.Text);
        Assert.Equal("alice", msg2.User);
        Assert.Equal("hello world", msg2.Text);
    }

    [Fact]
    public async Task BroadcastAsync_WithSingleClient_WritesMessage()
    {
        // Arrange
        var room = new ChatRoom();
        var channel = Channel.CreateUnbounded<ChatMessage>();
        room.AddClient(channel);

        var message = new ChatMessage { User = "bob", Text = "test message" };

        // Act
        var readTask = channel.Reader.ReadAsync().AsTask();
        await room.BroadcastAsync(message);
        var received = await readTask;

        // Assert
        Assert.Equal("bob", received.User);
        Assert.Equal("test message", received.Text);
    }

    [Fact]
    public async Task BroadcastAsync_WithNoClients_DoesNotThrow()
    {
        // Arrange
        var room = new ChatRoom();
        var message = new ChatMessage { User = "charlie", Text = "broadcast to empty room" };

        // Act & Assert
        await room.BroadcastAsync(message);
        // No exception should be thrown
    }

    [Fact]
    public async Task RemoveClient_PreventsReceivingMessages()
    {
        // Arrange
        var room = new ChatRoom();
        var channel1 = Channel.CreateUnbounded<ChatMessage>();
        var channel2 = Channel.CreateUnbounded<ChatMessage>();

        var id1 = room.AddClient(channel1);
        var id2 = room.AddClient(channel2);

        // Act
        room.RemoveClient(id1);

        var message = new ChatMessage { User = "dave", Text = "after remove" };
        var read2Task = channel2.Reader.ReadAsync().AsTask();

        await room.BroadcastAsync(message);

        var received2 = await read2Task;

        // Assert
        Assert.False(channel1.Reader.TryRead(out _), "Removed client should not receive message");
        Assert.Equal("after remove", received2.Text);
    }

    [Fact]
    public void RemoveClient_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var room = new ChatRoom();

        // Act & Assert
        room.RemoveClient("non-existent-id");
        // No exception should be thrown
    }

    [Fact]
    public async Task RemoveClient_RemovesMultipleClients()
    {
        // Arrange
        var room = new ChatRoom();
        var channel1 = Channel.CreateUnbounded<ChatMessage>();
        var channel2 = Channel.CreateUnbounded<ChatMessage>();
        var channel3 = Channel.CreateUnbounded<ChatMessage>();

        var id1 = room.AddClient(channel1);
        var id2 = room.AddClient(channel2);
        var id3 = room.AddClient(channel3);

        // Act
        room.RemoveClient(id1);
        room.RemoveClient(id2);

        var message = new ChatMessage { User = "eve", Text = "only for 3" };
        var read3Task = channel3.Reader.ReadAsync().AsTask();

        await room.BroadcastAsync(message);
        var received3 = await read3Task;

        // Assert
        Assert.False(channel1.Reader.TryRead(out _));
        Assert.False(channel2.Reader.TryRead(out _));
        Assert.Equal("only for 3", received3.Text);
    }

    [Fact]
    public async Task BroadcastAsync_IgnoresIndividualClientErrors()
    {
        // Arrange
        var room = new ChatRoom();
        var goodChannel = Channel.CreateUnbounded<ChatMessage>();
        var faultyChannel = Channel.CreateUnbounded<ChatMessage>();

        room.AddClient(goodChannel);
        room.AddClient(faultyChannel);

        // Complete the faulty channel to simulate a failure
        faultyChannel.Writer.Complete(new Exception("Simulated channel failure"));

        var message = new ChatMessage { User = "frank", Text = "resilient broadcast" };

        // Act & Assert
        await room.BroadcastAsync(message);

        var received = await goodChannel.Reader.ReadAsync();
        Assert.Equal("resilient broadcast", received.Text);
    }

    [Fact]
    public async Task BroadcastAsync_WithMultipleMessages_AllReachClients()
    {
        // Arrange
        var room = new ChatRoom();
        var channel = Channel.CreateUnbounded<ChatMessage>();
        room.AddClient(channel);

        // Act
        var messages = new[]
        {
            new ChatMessage { User = "user1", Text = "first" },
            new ChatMessage { User = "user2", Text = "second" },
            new ChatMessage { User = "user3", Text = "third" }
        };

        foreach (var msg in messages)
        {
            await room.BroadcastAsync(msg);
        }

        // Assert
        for (int i = 0; i < messages.Length; i++)
        {
            var received = await channel.Reader.ReadAsync();
            Assert.Equal(messages[i].User, received.User);
            Assert.Equal(messages[i].Text, received.Text);
        }
    }

    [Fact]
    public async Task BroadcastAsync_ConcurrentBroadcasts_AllMessagesSent()
    {
        // Arrange
        var room = new ChatRoom();
        var channels = new[] { Channel.CreateUnbounded<ChatMessage>(), Channel.CreateUnbounded<ChatMessage>() };
        room.AddClient(channels[0]);
        room.AddClient(channels[1]);

        // Act
        var broadcastTasks = new[]
        {
            room.BroadcastAsync(new ChatMessage { User = "user1", Text = "msg1" }),
            room.BroadcastAsync(new ChatMessage { User = "user2", Text = "msg2" }),
            room.BroadcastAsync(new ChatMessage { User = "user3", Text = "msg3" })
        };

        await Task.WhenAll(broadcastTasks);

        // Assert
        var messages0 = new List<ChatMessage>();
        var messages1 = new List<ChatMessage>();

        for (int i = 0; i < 3; i++)
        {
            messages0.Add(await channels[0].Reader.ReadAsync());
            messages1.Add(await channels[1].Reader.ReadAsync());
        }

        Assert.Equal(3, messages0.Count);
        Assert.Equal(3, messages1.Count);
    }

    [Fact]
    public async Task AddClient_AfterRemoveWithSameGuid_AllowsReuseOfId()
    {
        // Arrange
        var room = new ChatRoom();
        var channel1 = Channel.CreateUnbounded<ChatMessage>();
        var channel2 = Channel.CreateUnbounded<ChatMessage>();

        // Act
        var id1 = room.AddClient(channel1);
        room.RemoveClient(id1);
        var id2 = room.AddClient(channel2);

        var message = new ChatMessage { User = "grace", Text = "after reuse" };
        var readTask = channel2.Reader.ReadAsync().AsTask();

        await room.BroadcastAsync(message);
        var received = await readTask;

        // Assert - new client should receive the message
        Assert.Equal("after reuse", received.Text);
        Assert.False(channel1.Reader.TryRead(out _));
    }
}
