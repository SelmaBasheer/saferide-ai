using SafeRide.Analytics.Data;
using SafeRide.Analytics.Extensions;
using SafeRide.Analytics.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder
    .Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        )
    );
builder.Services.AddRouteOptions();
builder.Services.AddSwaggerDocs();

// AddJwtAuthentication already calls AddAuthorization internally.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// No AutoMapper. Dapper projects query results straight onto the report DTOs,
// so a mapping layer would sit between two shapes that are already identical.
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Before Build, so a failed migration stops the process rather than surfacing
// as a 500 on the first report someone runs.
DatabaseMigrator.Run(builder.Configuration);

var app = builder.Build();

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
