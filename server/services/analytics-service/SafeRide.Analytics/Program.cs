using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Common;
using SafeRide.Analytics.Consumers;
using SafeRide.Analytics.Data;
using SafeRide.Analytics.Data.Repositories;
using SafeRide.Analytics.Extensions;
using SafeRide.Analytics.Messaging;
using SafeRide.Analytics.Middleware;
using SafeRide.Analytics.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Registered before anything queries. Dapper caches deserialisers per type, so
// a handler added later would not apply to a type already materialised once.
Dapper.SqlMapper.AddTypeHandler(new SafeRide.Analytics.Data.DateOnlyTypeHandler());

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
builder.Services.AddSingleton<LocalDates>();

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName)
);

builder.Services.AddScoped<IBusDimensionRepository, BusDimensionRepository>();
builder.Services.AddScoped<ISchoolDimensionRepository, SchoolDimensionRepository>();
builder.Services.AddScoped<IPaymentFactRepository, PaymentFactRepository>();
builder.Services.AddScoped<ISuperAdminReportRepository, SuperAdminReportRepository>();
builder.Services.AddScoped<ITripFactRepository, TripFactRepository>();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<ISchoolReportRepository, SchoolReportRepository>();

builder.Services.AddScoped<SuperAdminReportService>();
builder.Services.AddScoped<SchoolReportService>();

builder.Services.AddHostedService<BusEventsConsumer>();
builder.Services.AddHostedService<SchoolEventsConsumer>();
builder.Services.AddHostedService<TrackingEventsConsumer>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Before Build, so a failed migration stops the process rather than surfacing
// as a 500 on the first report someone runs.
DatabaseMigrator.Run(builder.Configuration);

// QuestPDF is free under the Community licence below the revenue threshold, but
// it refuses to render until the choice is stated explicitly.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
