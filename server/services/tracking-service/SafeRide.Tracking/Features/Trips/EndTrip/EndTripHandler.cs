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

        await events.PublishAsync(
            MessagingConstants.TripEnded,
            new TripEndedEvent(
                ended.Id,
                ended.SchoolId,
                ended.RouteId,
                ended.BusId,
                ended.DriverId,
                ended.Roster.Count(r => r.BoardingStatus == BoardingStatus.Boarded),
                ended.Roster.Count(r => r.BoardingStatus == BoardingStatus.Absent),
                ended.UnmarkedCount,
                DateTime.UtcNow
            ),
            ct
        );

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
}
