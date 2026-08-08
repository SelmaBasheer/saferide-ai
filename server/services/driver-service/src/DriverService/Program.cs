using DriverService.Common;
using DriverService.Features.CreateDriver;
using DriverService.Features.GetDrivers;
using DriverService.Mapping;
using DriverService.Messaging;
using DriverService.Middleware;
using DriverService.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Persistence ----------
builder.Services.AddDbContext<DriverDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DriverDb"))
);

// ---------- AuthN / AuthZ ----------
builder.Services.AddJwtAuthentication(builder.Configuration);
builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("SchoolAdmin", p => p.RequireRole("SchoolAdmin"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// ---------- Validation ----------
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Mapping ----------
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<DriverMappingProfile>());

// ---------- Messaging (outbox → RabbitMQ) ----------
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OutboxRelay>();
builder.Services.AddHostedService<SchoolEventsConsumer>();

// ---------- API docs ----------
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar — dev-only test UI
}

// ---------- Endpoints (one line per slice) ----------
app.MapCreateDriver();
app.MapGetDrivers();

app.Run();
