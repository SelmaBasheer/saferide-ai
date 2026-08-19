using FluentValidation;
using Hangfire;
using SafeRide.Tracking.Features.Boarding.MarkBoarding;
using SafeRide.Tracking.Features.Gps.IngestPosition;
using SafeRide.Tracking.Features.Trips.EndTrip;
using SafeRide.Tracking.Features.Trips.GetActiveTrips;
using SafeRide.Tracking.Features.Trips.GetTrip;
using SafeRide.Tracking.Features.Trips.ListTrips;
using SafeRide.Tracking.Features.Trips.StartTrip;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Jobs;
using SafeRide.Tracking.Middleware;
using SafeRide.Tracking.Startup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("saferide-tracking");

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddRealtime(builder.Configuration);
builder.Services.AddSwaggerWithBearer();
builder.Services.AddFeatures(builder.Configuration);
builder.Services.AddJobs();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// ---------- Pipeline: order is load-bearing, kept visible on purpose ----------
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

RecurringJob.AddOrUpdate<DeviationCheckJob>(
    "deviation-check",
    job => job.RunAsync(CancellationToken.None),
    "*/2 * * * *"
);

// ---------- Endpoints: one line per slice ----------
app.MapStartTrip();
app.MapEndTrip();
app.MapGetActiveTrips();
app.MapGetTrip();
app.MapListTrips();
app.MapIngestPosition();
app.MapHub<TrackingHub>("/hubs/tracking");
app.MapMarkBoarding();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
