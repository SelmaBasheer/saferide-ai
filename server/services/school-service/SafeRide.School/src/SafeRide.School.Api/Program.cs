using SafeRide.School.Api.Extensions;
using SafeRide.School.Api.Middleware;
using SafeRide.School.Application;
using SafeRide.School.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddRouteOptions();
builder.Services.AddSwaggerDocs();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetryTracing();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.MapControllers();
app.Run();
