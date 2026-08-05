using DriverService.Common;
using DriverService.Features.CreateDriver;
using DriverService.Features.GetDrivers;
using DriverService.Messaging;
using DriverService.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DriverDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DriverDb"))
);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddOpenApi();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OutboxRelay>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar — the test UI
}
app.MapCreateDriver();
app.MapGetDrivers();

app.MapGet("/", () => "Hello World!");

app.UseAuthentication();
app.UseAuthorization();

app.Run();
