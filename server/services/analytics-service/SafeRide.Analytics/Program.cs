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

// ---------- Observability ----------
builder.AddSerilogLogging();

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
// Identity comes from the gateway's headers;
builder.Services.AddGatewayAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// ---------- Persistence ----------
// Registered before anything queries. Dapper caches deserialisers per type, so
// a handler added later would not apply to a type already materialised once.
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<LocalDates>();

// One interface per table rather than one repository for everything, so a
// consumer has no way to reach a table it has no business writing.
builder.Services.AddScoped<IBusDimensionRepository, BusDimensionRepository>();
builder.Services.AddScoped<ISchoolDimensionRepository, SchoolDimensionRepository>();
builder.Services.AddScoped<IPaymentFactRepository, PaymentFactRepository>();
builder.Services.AddScoped<ITripFactRepository, TripFactRepository>();
builder.Services.AddScoped<ISuperAdminReportRepository, SuperAdminReportRepository>();
builder.Services.AddScoped<ISchoolReportRepository, SchoolReportRepository>();

// ---------- Reporting ----------
// No AutoMapper. Dapper projects query results straight onto the report DTOs,
// so a mapping layer would sit between two shapes that are already identical.
builder.Services.AddScoped<SuperAdminReportService>();
builder.Services.AddScoped<SchoolReportService>();

// QuestPDF is free under the Community licence below the revenue threshold, but
// it refuses to render until the choice is stated explicitly.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ---------- Messaging (RabbitMQ → read model) ----------
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName)
);
builder.Services.AddHostedService<BusEventsConsumer>();
builder.Services.AddHostedService<SchoolEventsConsumer>();
builder.Services.AddHostedService<TrackingEventsConsumer>();

// ---------- Error handling ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------- Schema ----------
// Before Build, so a failed migration stops the process rather than surfacing
// as a 500 on the first report someone runs.
DatabaseMigrator.Run(builder.Configuration);

var app = builder.Build();

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
