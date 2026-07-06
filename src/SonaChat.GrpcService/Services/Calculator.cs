using System.Threading.Tasks;
using Grpc.Core;
using SonaChat.GrpcService.Protos; // keep or change to actual generated namespace

namespace SonaChat.GrpcService.Services
{
    public class CalculatorServiceImpl : CalculatorService.CalculatorServiceBase
    {
        public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            var result = request.A + request.B;
            var response = new AddResponse { Result = result };
            return Task.FromResult(response);
        }
    }
}