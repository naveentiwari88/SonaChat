using SonaChat.GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<ChatRoom>();

var app = builder.Build();

app.MapGrpcService<ChatService>();
app.MapGet("/", () => "SonaChat gRPC service is running.");

app.Run();
