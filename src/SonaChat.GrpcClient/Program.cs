using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

// Simple runnable client for the Calculator gRPC service.
// Usage: dotnet run --project src/SonaChat.GrpcClient [address] [http]
// Example (HTTPS): dotnet run --project src/SonaChat.GrpcClient
// Example (plaintext HTTP): dotnet run --project src/SonaChat.GrpcClient http://localhost:5000 http

try
{
    // Default to the service debug port configured in the solution
    var address = args.Length > 0 ? args[0] : "https://localhost:5002";
    var usePlainText = args.Length > 1 && args[1].Equals("http", StringComparison.OrdinalIgnoreCase);

    if (usePlainText)
    {
        // Allow unencrypted HTTP/2 for local development
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    Console.WriteLine($"Creating gRPC client for: {address} (plaintext={usePlainText})");

    // Before creating the gRPC channel, do a TCP reachability check with retries.
    async Task<bool> IsTcpPortOpenWithRetriesAsync(string host, int port, int attempts = 5, int delayMs = 300)
    {
        string[] hostsToTry = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? new[] { "127.0.0.1", "::1", "localhost" }
            : new[] { host };

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            foreach (var h in hostsToTry)
            {
                try
                {
                    using var c = new TcpClient();
                    var connectTask = c.ConnectAsync(h, port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(1000));
                    if (completed == connectTask && c.Connected)
                    {
                        return true;
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }

            await Task.Delay(delayMs);
        }

        return false;
    }

    // No pre-check: attempt RPC and show any errors directly

    using var client = new SonaChat.GrpcClient.CalculatorClient(address);

    Console.WriteLine("Calling Add(1.5, 2.5)...");
    var result = await client.AddAsync(1.5, 2.5);
    Console.WriteLine($"Result: {result}");

    // Exit immediately after the call when running non-interactively
    Console.WriteLine("Client finished.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error running client: {ex.Message}");
}
