using SonaChat.GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);
// Bind to an alternate port to avoid "address already in use" exceptions during debugging
// Prefer localhost (matches developer HTTPS dev certificate) and a non-default port to avoid conflicts
builder.WebHost.UseUrls("https://localhost:5002");

builder.Services.AddGrpc();
builder.Services.AddSingleton<ChatRoom>();

var app = builder.Build();

app.MapGrpcService<ChatService>();
app.MapGrpcService<SonaChat.GrpcService.Services.CalculatorServiceImpl>();
app.MapGet("/", () => "SonaChat gRPC service is running.");

app.Run();
