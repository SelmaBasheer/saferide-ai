using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;

namespace SafeRide.Tracking.Jobs;

public sealed class DeviationCheckJob(
    IMongoCollection<Trip> trips,
    IHubContext<TrackingHub, ITrackingClient> hub,
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
            if (trip.LastPosition is null || trip.Route.Path is null)
            {
                continue;
            }

            if (!trip.ShouldAlertDeviation(cooldown))
            {
                continue;
            }

            var path = trip
                .Route.Path.Coordinates.Positions.Select(p => (p.Latitude, p.Longitude))
                .ToList();

            var lat = trip.LastPosition.Location.Latitude();
            var lon = trip.LastPosition.Location.Longitude();
            var offRoute = GeoDistance.MetresToPath(lat, lon, path);

            if (offRoute <= settings.DeviationThresholdMetres)
            {
                continue;
            }

            trip.MarkDeviationAlerted();

            await trips.UpdateOneAsync(
                t => t.Id == trip.Id,
                Builders<Trip>
                    .Update.Set(t => t.DeviationAlertedAt, trip.DeviationAlertedAt)
                    .Set(t => t.UpdatedAt, trip.UpdatedAt),
                cancellationToken: ct
            );

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
        }
    }
}
