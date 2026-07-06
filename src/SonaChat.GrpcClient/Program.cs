using System;
using System.Net.Http;
using System.Threading.Tasks;

// Simple runnable client for the Calculator gRPC service.
// Usage: dotnet run --project src/SonaChat.GrpcClient [address] [http]
// Example (HTTPS): dotnet run --project src/SonaChat.GrpcClient
// Example (plaintext HTTP): dotnet run --project src/SonaChat.GrpcClient http://localhost:5000 http

try
{
    var address = args.Length > 0 ? args[0] : "https://localhost:5001";
    var usePlainText = args.Length > 1 && args[1].Equals("http", StringComparison.OrdinalIgnoreCase);

    if (usePlainText)
    {
        // Allow unencrypted HTTP/2 for local development
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    Console.WriteLine($"Creating gRPC client for: {address} (plaintext={usePlainText})");

    using var client = new SonaChat.GrpcClient.CalculatorClient(address);

    Console.WriteLine("Calling Add(1.5, 2.5)...");
    var result = await client.AddAsync(1.5, 2.5);
    Console.WriteLine($"Result: {result}");

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey(true);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error running client: {ex.Message}");
}
