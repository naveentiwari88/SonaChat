using System.Threading.Channels;
using Grpc.Net.Client;
using Grpc.Core;
using SonaChat.Protos;

namespace SonaChat.Web.Services;

public class GrpcChatClient : IAsyncDisposable
{
    private readonly Channel<ChatMessage> _received = Channel.CreateUnbounded<ChatMessage>();
    private AsyncDuplexStreamingCall<ChatMessage, ChatMessage>? _call;
    private GrpcChannel? _channel;

    public IAsyncEnumerable<ChatMessage> ReceiveAllAsync() => _received.Reader.ReadAllAsync();

    public async Task StartAsync(CancellationToken ct = default)
    {
        // ensure only one channel
        if (_call != null) return;

        _channel = GrpcChannel.ForAddress("https://localhost:5001");
        var client = new Chat.ChatClient(_channel);
        _call = client.Connect(cancellationToken: ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in _call.ResponseStream.ReadAllAsync(ct))
                {
                    await _received.Writer.WriteAsync(msg, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                _received.Writer.TryComplete();
            }
        }, ct);
    }

    public async Task SendAsync(string user, string text)
    {
        if (_call == null) throw new InvalidOperationException("Client not started");
        var msg = new ChatMessage { User = user, Text = text, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        await _call.RequestStream.WriteAsync(msg);
    }

    public async Task StopAsync()
    {
        if (_call != null)
        {
            try { await _call.RequestStream.CompleteAsync(); } catch { }
            _call = null;
        }

        if (_channel != null)
        {
            await _channel.ShutdownAsync();
            _channel = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
