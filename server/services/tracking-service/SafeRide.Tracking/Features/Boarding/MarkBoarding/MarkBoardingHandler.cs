using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using SafeRide.Tracking.Infrastructure.Messaging;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Boarding.MarkBoarding;

public sealed class MarkBoardingHandler(
    IMongoCollection<Trip> trips,
    IHubContext<TrackingHub, ITrackingClient> hub,
    IEventPublisher events,
    ILogger<MarkBoardingHandler> logger
)
{
    public async Task<TripResponse> HandleAsync(
        Guid tripId,
        MarkBoardingRequest request,
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var trip =
            await trips.Find(t => t.Id == tripId).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Trip not found.");

        if (trip.SchoolId != user.SchoolId() || trip.DriverId != user.UserId())
        {
            throw AppException.NotFound("Trip not found.");
        }

        var status = Enum.Parse<BoardingStatus>(request.Status, true);
        var entry = trip.MarkBoarding(request.StudentId, status, user.UserId());

        var filter = Builders<Trip>.Filter.And(
            Builders<Trip>.Filter.Eq(t => t.Id, trip.Id),
            Builders<Trip>.Filter.ElemMatch(t => t.Roster, r => r.StudentId == request.StudentId)
        );

        var update = Builders<Trip>
            .Update.Set(t => t.Roster.FirstMatchingElement().BoardingStatus, entry.BoardingStatus)
            .Set(t => t.Roster.FirstMatchingElement().MarkedAt, entry.MarkedAt)
            .Set(t => t.Roster.FirstMatchingElement().MarkedBy, entry.MarkedBy)
            .Set(t => t.UpdatedAt, trip.UpdatedAt);

        await trips.UpdateOneAsync(filter, update, cancellationToken: ct);

        var stop = trip.Route.Stops.FirstOrDefault(s => s.StopId == entry.PickupStopId);
        var stopName = stop?.Name ?? "their stop";

        await hub
            .Clients.User(entry.ParentEmail)
            .StudentBoarded(
                new StudentBoardedNotification(
                    trip.Id,
                    entry.StudentId,
                    entry.Name,
                    stopName,
                    entry.BoardingStatus.ToString(),
                    entry.MarkedAt!.Value
                )
            );

        await events.PublishAsync(
            MessagingConstants.StudentBoarded,
            new StudentBoardedEvent(
                trip.Id,
                trip.SchoolId,
                entry.StudentId,
                entry.PickupStopId,
                stopName,
                entry.BoardingStatus.ToString(),
                DateTime.UtcNow
            ),
            ct
        );

        logger.LogInformation(
            "Trip {TripId}: student {StudentId} marked {Status}",
            trip.Id,
            entry.StudentId,
            entry.BoardingStatus
        );

        return trip.ToResponse();
    }
}
