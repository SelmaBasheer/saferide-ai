using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using SafeRide.Tracking.Infrastructure.Messaging;

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

        trip.End();

        await trips.ReplaceOneAsync(t => t.Id == trip.Id, trip, cancellationToken: ct);

        await hub
            .Clients.Groups(TrackingHub.TripGroup(trip.Id), TrackingHub.SchoolGroup(trip.SchoolId))
            .TripEnded(
                new TripLifecycleNotification(
                    trip.Id,
                    trip.BusId,
                    trip.Route.Code,
                    trip.Route.Name,
                    trip.EndedAt!.Value
                )
            );

        await events.PublishAsync(
            MessagingConstants.TripEnded,
            new TripEndedEvent(
                trip.Id,
                trip.SchoolId,
                trip.RouteId,
                trip.BusId,
                trip.DriverId,
                trip.Roster.Count(r => r.BoardingStatus == BoardingStatus.Boarded),
                trip.Roster.Count(r => r.BoardingStatus == BoardingStatus.Absent),
                trip.UnmarkedCount,
                DateTime.UtcNow
            ),
            ct
        );

        logger.LogInformation(
            "Trip {TripId} ended with {UnmarkedCount} students unmarked",
            trip.Id,
            trip.UnmarkedCount
        );

        return trip.ToResponse();
    }
}
