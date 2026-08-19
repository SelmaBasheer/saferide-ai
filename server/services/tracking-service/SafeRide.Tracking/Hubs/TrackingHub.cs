using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Features.Gps.IngestPosition;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Hubs;

[Authorize]
public sealed class TrackingHub(
    IMongoCollection<Trip> trips,
    IngestPositionHandler ingest,
    ILogger<TrackingHub> logger
) : Hub<ITrackingClient>
{
    public async Task SendPosition(
        Guid tripId,
        double latitude,
        double longitude,
        double? speedKmh,
        string? source
    )
    {
        PositionSource parsedSource;

        if (string.IsNullOrWhiteSpace(source))
        {
            parsedSource = PositionSource.Gps;
        }
        else if (!Enum.TryParse(source, true, out parsedSource) || !Enum.IsDefined(parsedSource))
        {
            throw new HubException($"Unknown position source '{source}'.");
        }

        await ingest.HandleAsync(
            tripId,
            Context.User!.UserId(),
            new PositionInput(latitude, longitude, speedKmh, parsedSource),
            Context.ConnectionAborted
        );
    }

    public async Task JoinTrip(Guid tripId)
    {
        var trip =
            await trips.Find(t => t.Id == tripId).FirstOrDefaultAsync()
            ?? throw new HubException("Trip not found.");

        if (!TripAccess.CanView(trip, Context.User!))
        {
            logger.LogWarning("Rejected join for trip {TripId}", tripId);
            throw new HubException("Trip not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TripGroup(tripId));
    }

    public Task LeaveTrip(Guid tripId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TripGroup(tripId));

    public async Task JoinSchoolFleet()
    {
        if (!Context.User!.IsInRole("SchoolAdmin"))
        {
            throw new HubException("Not permitted.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SchoolGroup(Context.User!.SchoolId()));
    }

    public Task LeaveSchoolFleet() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, SchoolGroup(Context.User!.SchoolId()));

    public static string TripGroup(Guid tripId) => $"trip-{tripId}";

    public static string SchoolGroup(Guid schoolId) => $"school-{schoolId}";
}
