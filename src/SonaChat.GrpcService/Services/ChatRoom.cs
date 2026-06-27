using System.Collections.Concurrent;
using System.Threading.Channels;
using SonaChat.Protos;

namespace SonaChat.GrpcService.Services;

public class ChatRoom
{
    private readonly ConcurrentDictionary<string, Channel<ChatMessage>> _clients = new();

    public string AddClient(Channel<ChatMessage> channel)
    {
        var id = Guid.NewGuid().ToString();
        _clients[id] = channel;
        return id;
    }

    public void RemoveClient(string id)
    {
        _clients.TryRemove(id, out _);
    }

    public async Task BroadcastAsync(ChatMessage message)
    {
        var writeTasks = _clients.Values.Select(async ch =>
        {
            try
            {
                await ch.Writer.WriteAsync(message);
            }
            catch
            {
                // ignore individual client errors
            }
        });

        await Task.WhenAll(writeTasks);
    }
}
