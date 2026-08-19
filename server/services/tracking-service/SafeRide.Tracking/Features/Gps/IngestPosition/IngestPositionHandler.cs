using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using Serilog.Core;

namespace SafeRide.Tracking.Features.Gps.IngestPosition;

public sealed record PositionInput(
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    PositionSource Source
);

public sealed class IngestPositionHandler(
    IMongoCollection<Trip> trips,
    IMongoCollection<GpsPoint> points,
    IHubContext<TrackingHub, ITrackingClient> hub,
    IOptions<TrackingOptions> options,
    ILogger<IngestPositionHandler> logger
)
{
    public async Task HandleAsync(
        Guid tripId,
        Guid callerId,
        PositionInput input,
        CancellationToken ct
    )
    {
        var trip =
            await trips.Find(t => t.Id == tripId).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Trip not found.");

        if (trip.DriverId != callerId)
        {
            throw AppException.NotFound("Trip not found.");
        }

        trip.RecordPosition(input.Latitude, input.Longitude, input.SpeedKmh, input.Source);

        var update = Builders<Trip>
            .Update.Set(t => t.LastPosition, trip.LastPosition)
            .Set(t => t.UpdatedAt, trip.UpdatedAt);

        await trips.UpdateOneAsync(t => t.Id == trip.Id, update, cancellationToken: ct);

        await points.InsertOneAsync(
            new GpsPoint
            {
                TripId = trip.Id,
                SchoolId = trip.SchoolId,
                Location = trip.LastPosition!.Location,
                RecordedAt = trip.LastPosition.RecordedAt,
                SpeedKmh = trip.LastPosition.SpeedKmh,
                Source = trip.LastPosition.Source,
            },
            cancellationToken: ct
        );

        var payload = new PositionUpdate(
            trip.Id,
            trip.BusId,
            trip.Route.Code,
            input.Latitude,
            input.Longitude,
            input.SpeedKmh,
            trip.LastPosition.RecordedAt,
            trip.LastPosition.Source.ToString()
        );

        await hub
            .Clients.Groups(TrackingHub.TripGroup(trip.Id), TrackingHub.SchoolGroup(trip.SchoolId))
            .PositionUpdated(payload);

        await CheckGeofenceAsync(trip, input.Latitude, input.Longitude, ct);
    }

    private async Task CheckGeofenceAsync(
        Trip trip,
        double latitude,
        double longitude,
        CancellationToken ct
    )
    {
        var settings = options.Value;

        var reached = trip
            .Route.Stops.Where(s => s.ReachedAt is null)
            .Select(s => new
            {
                Stop = s,
                Distance = GeoDistance.Metres(
                    latitude,
                    longitude,
                    s.Location.Latitude(),
                    s.Location.Longitude()
                ),
            })
            .Where(x => x.Distance <= settings.GeofenceRadiusMetres)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (
            reached is null
            || !trip.TryMarkStopReached(reached.Stop.StopId, out var stop)
            || stop is null
        )
        {
            return;
        }

        var stopFilter = Builders<Trip>.Filter.And(
            Builders<Trip>.Filter.Eq(t => t.Id, trip.Id),
            Builders<Trip>.Filter.ElemMatch(
                t => t.Route.Stops,
                s => s.StopId == stop.StopId && s.ReachedAt == null
            )
        );

        var result = await trips.UpdateOneAsync(
            stopFilter,
            Builders<Trip>.Update.Set(
                t => t.Route.Stops.FirstMatchingElement().ReachedAt,
                stop.ReachedAt
            ),
            cancellationToken: ct
        );

        if (result.ModifiedCount == 0)
        {
            return; // another position post won the race
        }

        await trips.UpdateOneAsync(
            stopFilter,
            Builders<Trip>.Update.Set(
                t => t.Route.Stops.FirstMatchingElement().ReachedAt,
                stop.ReachedAt
            ),
            cancellationToken: ct
        );

        logger.LogInformation(
            "Trip {TripId} reached stop {Sequence} — {StopName}",
            trip.Id,
            stop.Sequence,
            stop.Name
        );

        await hub
            .Clients.Group(TrackingHub.TripGroup(trip.Id))
            .StopReached(
                new StopReachedNotification(
                    trip.Id,
                    stop.StopId,
                    stop.Name,
                    stop.Sequence,
                    stop.ReachedAt!.Value
                )
            );

        var ahead = trip.StopAhead(stop.Sequence, settings.ApproachStopsAhead);
        if (ahead is null)
        {
            return;
        }

        var parents = trip.StudentsAtStop(ahead.StopId)
            .Select(r => r.ParentEmail)
            .Distinct()
            .ToList();

        if (parents.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Notifying {ParentCount} parents that the bus is approaching {StopName}",
            parents.Count,
            ahead.Name
        );

        await hub
            .Clients.Users(parents)
            .ApproachingStop(
                new ApproachingStopNotification(
                    trip.Id,
                    trip.Route.Code,
                    ahead.StopId,
                    ahead.Name,
                    ahead.Sequence,
                    settings.ApproachStopsAhead
                )
            );
    }
}
