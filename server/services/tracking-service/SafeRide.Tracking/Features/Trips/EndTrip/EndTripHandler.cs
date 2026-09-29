using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using SafeRide.Tracking.Infrastructure.Messaging;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Trips.EndTrip;

public sealed class EndTripHandler(
    IMongoCollection<Trip> trips,
    IHubContext<TrackingHub, ITrackingClient> hub,
    IEventPublisher events,
    ILogger<EndTripHandler> logger
)
{
    public async Task<TripResponse> HandleAsync(
        Guid tripId,
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var trip =
            await trips.Find(t => t.Id == tripId).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Trip not found.");

        if (!TripAccess.CanEnd(trip, user))
        {
            throw AppException.NotFound("Trip not found.");
        }

        var now = DateTime.UtcNow;

        var ended = await trips.FindOneAndUpdateAsync(
            Builders<Trip>.Filter.And(
                Builders<Trip>.Filter.Eq(t => t.Id, tripId),
                Builders<Trip>.Filter.Eq(t => t.Status, TripStatus.Active)
            ),
            Builders<Trip>
                .Update.Set(t => t.Status, TripStatus.Completed)
                .Set(t => t.EndedAt, now)
                .Set(t => t.UpdatedAt, now),
            new FindOneAndUpdateOptions<Trip> { ReturnDocument = ReturnDocument.After },
            ct
        );

        if (ended is null)
        {
            throw AppException.Conflict("Trip is not active.");
        }

        await events.PublishAsync(MessagingConstants.TripEnded, ToEvent(ended), ct);

        await PublishSkippedStopsAsync(ended, ct);

        await hub
            .Clients.Groups(
                TrackingHub.TripGroup(ended.Id),
                TrackingHub.SchoolGroup(ended.SchoolId)
            )
            .TripEnded(
                new TripLifecycleNotification(
                    ended.Id,
                    ended.BusId,
                    ended.Route.Code,
                    ended.Route.Name,
                    ended.EndedAt!.Value
                )
            );

        logger.LogInformation(
            "Trip {TripId} ended with {UnmarkedCount} students unmarked",
            ended.Id,
            ended.UnmarkedCount
        );

        return ended.ToResponse();
    }

    /// <summary>
    /// Builds the event from the finished trip. The stop name is resolved here,
    /// from the route already loaded on the trip, so the consumer never has to
    /// know that a roster entry holds a stop id rather than a stop.
    /// </summary>
    private static TripEndedEvent ToEvent(Trip ended)
    {
        var stopNames = ended.Route.Stops.ToDictionary(s => s.StopId, s => s.Name);

        var roster = ended
            .Roster.Select(r => new TripRosterEntry(
                r.StudentId,
                r.Name,
                r.PickupStopId,
                stopNames.GetValueOrDefault(r.PickupStopId),
                r.BoardingStatus.ToString(),
                r.MarkedAt
            ))
            .ToList();

        return new TripEndedEvent(
            ended.Id,
            ended.SchoolId,
            ended.RouteId,
            ended.BusId,
            ended.DriverId,
            ended.Route.Code,
            ended.Route.Name,
            ended.StartedAt,
            ended.EndedAt!.Value,
            ended.Roster.Count,
            ended.Roster.Count(r => r.BoardingStatus == BoardingStatus.Boarded),
            ended.Roster.Count(r => r.BoardingStatus == BoardingStatus.Absent),
            ended.UnmarkedCount,
            roster,
            DateTime.UtcNow
        );
    }

    /// A stop with no ReachedAt is one the geofence never matched, so the bus
    /// never came within 100 m of it. Unlike a deviation, this is not a judgement
    /// call and there is no threshold to tune — the stop was reached or it wasn't.
    private async Task PublishSkippedStopsAsync(Trip ended, CancellationToken ct)
    {
        var skipped = ended
            .Route.Stops.Where(s => s.ReachedAt is null)
            .OrderBy(s => s.Sequence)
            .Select(s => new SkippedStopInfo(s.Sequence, s.Name, s.PickupTime))
            .ToList();

        if (skipped.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            "Trip {TripId} ended with {SkippedCount} of {Total} stops never reached",
            ended.Id,
            skipped.Count,
            ended.Route.Stops.Count
        );

        try
        {
            await events.PublishAsync(
                MessagingConstants.StopsSkipped,
                new StopsSkippedDetected(
                    Guid.NewGuid(),
                    ended.Id,
                    ended.SchoolId,
                    ended.BusId,
                    ended.DriverId,
                    ended.Route.Code,
                    ended.Route.Name,
                    ended.Route.Stops.Count,
                    ended.Route.Stops.Count - skipped.Count,
                    skipped,
                    ended.StartedAt,
                    ended.EndedAt!.Value,
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            // The trip has already been completed in the database and the driver
            // has their response. A broker outage must not turn a successful
            // "end trip" into an error on the driver's phone.
            logger.LogError(ex, "Failed to publish skipped stops for trip {TripId}", ended.Id);
        }
    }
}
