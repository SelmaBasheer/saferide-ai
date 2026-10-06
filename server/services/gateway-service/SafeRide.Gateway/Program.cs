using SafeRide.Gateway.Extensions;
using SafeRide.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyVault();

builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayProxy(builder.Configuration);
builder.Services.AddProxyHeaders();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseErrorEnvelope();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapReverseProxy();

app.Run();
