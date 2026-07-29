using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Api.Extensions;
using SafeRide.Schools.Api.Mapping;
using SafeRide.Schools.Api.Middleware;
using SafeRide.Schools.Application;
using SafeRide.Schools.Infrastructure;
using SafeRide.Schools.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddRouteOptions();
builder.Services.AddSwaggerDocs();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetryTracing();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<SchoolMappingProfile>());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
    await db.Database.MigrateAsync();
}

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
