using Grpc.Core;
using SonaChat.Protos;

namespace SonaChat.GrpcService.Services;

public class ChatService : Chat.ChatBase
{
    private readonly ChatRoom _room;

    public ChatService(ChatRoom room)
    {
        _room = room;
    }

    public override async Task Connect(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<ChatMessage>();
        var clientId = _room.AddClient(channel);

        // Task: pump outgoing messages to response stream
        var outgoing = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in channel.Reader.ReadAllAsync(context.CancellationToken))
                {
                    await responseStream.WriteAsync(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }, context.CancellationToken);

        // Task: read incoming messages from this client and broadcast
        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var msg = requestStream.Current;
                // ensure timestamp
                if (msg.Timestamp == 0)
                    msg.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                await _room.BroadcastAsync(msg);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _room.RemoveClient(clientId);
            try { channel.Writer.Complete(); } catch { }
            await outgoing;
        }
    }
}
