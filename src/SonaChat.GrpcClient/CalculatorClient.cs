using Grpc.Net.Client;
using SonaChat.GrpcService.Protos;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SonaChat.GrpcClient
{
    /// <summary>
    /// Simple wrapper around the generated CalculatorService client.
    /// Create an instance with the server address (e.g. "https://localhost:5001").
    /// </summary>
    public sealed class CalculatorClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly CalculatorService.CalculatorServiceClient _client;
        private bool _disposed;

        public CalculatorClient(string address)
        {
            _channel = GrpcChannel.ForAddress(address);
            _client = new CalculatorService.CalculatorServiceClient(_channel);
        }

        public async Task<double> AddAsync(double a, double b)
        {
        
            var request = new AddRequest { A = a, B = b };
            var response = await _client.AddAsync(request).ResponseAsync;
            return response.Result;
        }

        public double Add(double a, double b)
        {
            var request = new AddRequest { A = a, B = b };
            var response = _client.Add(request);
            return response.Result;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _channel?.Dispose();
            _disposed = true;
        }
    }
}
