using Microsoft.EntityFrameworkCore;
using SafeRide.Ai.Api.Common;
using SafeRide.Ai.Api.Extensions;
using SafeRide.Ai.Api.Middleware;
using SafeRide.Ai.Application;
using SafeRide.Ai.Infrastructure;
using SafeRide.Ai.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Observability ----------
builder.AddSerilogLogging();

// ---------- API ----------
builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();

// ---------- AuthN / AuthZ ----------
builder.Services.AddGatewayAuthentication();
builder.Services.AddAuthorizationPolicies();

// ---------- Application & Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Error handling ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------- Schema ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
    await db.Database.MigrateAsync();
}

// ---------- Middleware pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
