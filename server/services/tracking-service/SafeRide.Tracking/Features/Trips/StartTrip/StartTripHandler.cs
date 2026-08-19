using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Trips.Contracts;
using SafeRide.Tracking.Hubs;
using SafeRide.Tracking.Hubs.Contracts;
using SafeRide.Tracking.Infrastructure;
using SafeRide.Tracking.Infrastructure.Messaging;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Trips.StartTrip;

public sealed class StartTripHandler(
    IMongoCollection<Trip> trips,
    IHubContext<TrackingHub, ITrackingClient> hub,
    RouteClient routeClient,
    StudentClient studentClient,
    IEventPublisher events,
    ILogger<StartTripHandler> logger
)
{
    public async Task<TripResponse> HandleAsync(
        StartTripRequest request,
        ClaimsPrincipal user,
        CancellationToken ct
    )
    {
        var schoolId = user.SchoolId();
        var driverId = user.UserId();

        var existing = await trips
            .Find(t => t.DriverId == driverId && t.Status == TripStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            throw AppException.Conflict("You already have a trip in progress.");
        }

        var route = await routeClient.GetRouteAsync(request.RouteId, ct);

        if (!string.Equals(route.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Validation("This route is not active.");
        }

        if (route.AssignedBusId is null)
        {
            throw AppException.Validation("This route has no bus assigned.");
        }

        if (route.Stops.Count == 0)
        {
            throw AppException.Validation("This route has no stops.");
        }

        var roster = await studentClient.GetRosterAsync(request.RouteId, ct);

        if (roster.Count == 0)
        {
            logger.LogWarning("Route {RouteId} has no students assigned", request.RouteId);
        }

        var tripRoute = new TripRoute
        {
            Code = route.Code,
            Name = route.Name,
            Stops =
            [
                .. route
                    .Stops.OrderBy(s => s.Sequence)
                    .Select(s => new TripStop
                    {
                        StopId = s.StopId,
                        Sequence = s.Sequence,
                        Name = s.Name,
                        Location = Geo.Point(s.Latitude, s.Longitude),
                        PickupTime = s.PickupTime,
                    }),
            ],
            Path = route.Path is { Count: > 1 }
                ? Geo.Line(route.Path.Select(p => (p.Latitude, p.Longitude)))
                : null,
        };

        var entries = roster
            .Select(r => new RosterEntry
            {
                StudentId = r.StudentId,
                Name = $"{r.FirstName} {r.LastName}".Trim(),
                ParentEmail = r.ParentEmail.ToLowerInvariant(),
                PickupStopId = r.PickupStopId,
            })
            .ToList();

        var trip = Trip.Start(
            schoolId,
            request.RouteId,
            route.AssignedBusId.Value,
            driverId,
            tripRoute,
            entries
        );

        try
        {
            await trips.InsertOneAsync(trip, cancellationToken: ct);
        }
        catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw AppException.Conflict("You already have a trip in progress.");
        }

        await events.PublishAsync(
            MessagingConstants.TripStarted,
            new TripStartedEvent(
                trip.Id,
                schoolId,
                request.RouteId,
                route.AssignedBusId.Value,
                driverId,
                route.Code,
                entries.Count,
                DateTime.UtcNow
            ),
            ct
        );

        await hub
            .Clients.Group(TrackingHub.SchoolGroup(schoolId))
            .TripStarted(
                new TripLifecycleNotification(
                    trip.Id,
                    trip.BusId,
                    route.Code,
                    route.Name,
                    trip.StartedAt
                )
            );

        logger.LogInformation(
            "Trip {TripId} started on route {RouteCode} with {StudentCount} students",
            trip.Id,
            route.Code,
            entries.Count
        );

        return trip.ToResponse();
    }
}
