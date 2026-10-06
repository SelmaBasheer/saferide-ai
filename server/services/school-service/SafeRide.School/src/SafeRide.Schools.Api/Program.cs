using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Extensions;
using SafeRide.Schools.Api.Mapping;
using SafeRide.Schools.Api.Middleware;
using SafeRide.Schools.Application;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Infrastructure;
using SafeRide.Schools.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Observability ----------
builder.AddSerilogLogging();
builder.Services.AddOpenTelemetryTracing();

// ---------- API ----------
builder
    .Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        )
    );
builder.Services.AddRouteOptions();
builder.Services.AddSwaggerDocs();

// ---------- AuthN / AuthZ ----------
// Razorpay webhook stays [AllowAnonymous] — it arrives on a gateway route with
// no authorization policy, so no identity headers are added to it.
builder.Services.AddGatewayAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// ---------- Application & Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Mapping ----------
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<SchoolMappingProfile>());

// ---------- Error handling ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------- Startup checks ----------
// Fails here rather than on the first request that happens to hit a bad map.
app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
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
