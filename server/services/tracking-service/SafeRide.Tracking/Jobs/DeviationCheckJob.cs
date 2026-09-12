using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using SafeRide.Tracking.Infrastructure.Messaging;

namespace SafeRide.Tracking.Jobs;

public sealed class DeviationCheckJob(
    IMongoCollection<Trip> trips,
    IHubContext<TrackingHub, ITrackingClient> hub,
    IEventPublisher events,
    IOptions<TrackingOptions> options,
    ILogger<DeviationCheckJob> logger
)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var cooldown = TimeSpan.FromMinutes(settings.DeviationCooldownMinutes);

        var active = await trips.Find(t => t.Status == TripStatus.Active).ToListAsync(ct);

        foreach (var trip in active)
        {
            try
            {
                await CheckTripAsync(trip, settings, cooldown, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deviation check failed for trip {TripId}", trip.Id);
            }
        }
    }

    private async Task CheckTripAsync(
        Trip trip,
        TrackingOptions settings,
        TimeSpan cooldown,
        CancellationToken ct
    )
    {
        if (trip.LastPosition is null || trip.Route.Path is null)
        {
            return;
        }

        if (!trip.ShouldAlertDeviation(cooldown))
        {
            return;
        }

        var path = trip
            .Route.Path.Coordinates.Positions.Select(p => (p.Latitude, p.Longitude))
            .ToList();

        if (path.Count < 2)
        {
            logger.LogDebug("Trip {TripId} has no usable path, skipping", trip.Id);
            return;
        }

        var lat = trip.LastPosition.Location.Latitude();
        var lon = trip.LastPosition.Location.Longitude();
        var offRoute = GeoDistance.MetresToPath(lat, lon, path);

        if (offRoute <= settings.DeviationThresholdMetres)
        {
            return;
        }

        logger.LogWarning(
            "Trip {TripId} is {Metres:0} m off route {RouteCode}",
            trip.Id,
            offRoute,
            trip.Route.Code
        );

        await hub
            .Clients.Group(TrackingHub.SchoolGroup(trip.SchoolId))
            .RouteDeviation(
                new RouteDeviationNotification(
                    trip.Id,
                    trip.BusId,
                    trip.Route.Code,
                    lat,
                    lon,
                    Math.Round(offRoute),
                    DateTime.UtcNow
                )
            );

        try
        {
            await events.PublishAsync(
                MessagingConstants.RouteDeviationDetected,
                new RouteDeviationDetected(
                    trip.Id,
                    trip.SchoolId,
                    trip.BusId,
                    trip.DriverId,
                    trip.Route.Code,
                    trip.Route.Name,
                    lat,
                    lon,
                    Math.Round(offRoute),
                    trip.LastPosition.SpeedKmh,
                    trip.Route.Stops.Count,
                    trip.Route.Stops.Count(s => s.ReachedAt is not null),
                    trip.StartedAt,
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            // The live alert has already reached the school admin. AI classification
            // is an enhancement — a broker outage must not cost us the alert itself.
            logger.LogError(ex, "Failed to publish deviation event for trip {TripId}", trip.Id);
        }

        trip.MarkDeviationAlerted();

        await trips.UpdateOneAsync(
            t => t.Id == trip.Id,
            Builders<Trip>
                .Update.Set(t => t.DeviationAlertedAt, trip.DeviationAlertedAt)
                .Set(t => t.UpdatedAt, trip.UpdatedAt),
            cancellationToken: ct
        );
    }
}
