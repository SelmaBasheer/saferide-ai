using SafeRide.Tracking.Domain;

namespace SafeRide.Tracking.Features.Trips.Contracts;

public static class TripMapping
{
    /// Maps a trip to its response. Pass <paramref name="roster"/> to narrow which
    /// students are included — a parent must only see their own children.
    public static TripResponse ToResponse(this Trip trip, IEnumerable<RosterEntry>? roster = null)
    {
        var entries = (roster ?? trip.Roster).ToList();

        return new TripResponse(
            trip.Id,
            trip.SchoolId,
            trip.RouteId,
            trip.BusId,
            trip.DriverId,
            trip.Status.ToString(),
            trip.StartedAt,
            trip.EndedAt,
            trip.Route.Code,
            trip.Route.Name,
            [
                .. trip.Route.Stops.Select(s => new TripStopResponse(
                    s.StopId,
                    s.Sequence,
                    s.Name,
                    s.Location.Latitude(),
                    s.Location.Longitude(),
                    s.PickupTime,
                    s.ReachedAt
                )),
            ],
            [
                .. trip.Route.Path?.Coordinates.Positions.Select(p => new GeoPointResponse(
                    p.Latitude,
                    p.Longitude
                )) ?? [],
            ],
            [
                .. entries.Select(r => new TripRosterResponse(
                    r.StudentId,
                    r.Name,
                    r.PickupStopId,
                    r.BoardingStatus.ToString(),
                    r.MarkedAt
                )),
            ],
            trip.LastPosition is null
                ? null
                : new LastPositionResponse(
                    trip.LastPosition.Location.Latitude(),
                    trip.LastPosition.Location.Longitude(),
                    trip.LastPosition.RecordedAt,
                    trip.LastPosition.SpeedKmh,
                    trip.LastPosition.Source.ToString()
                ),
            entries.Count(e => e.BoardingStatus == BoardingStatus.Unmarked)
        );
    }

    public static TripSummaryResponse ToSummary(this Trip trip) =>
        new(
            trip.Id,
            trip.RouteId,
            trip.BusId,
            trip.DriverId,
            trip.Status.ToString(),
            trip.StartedAt,
            trip.EndedAt,
            trip.Route.Code,
            trip.Route.Name,
            trip.LastPosition is null
                ? null
                : new LastPositionResponse(
                    trip.LastPosition.Location.Latitude(),
                    trip.LastPosition.Location.Longitude(),
                    trip.LastPosition.RecordedAt,
                    trip.LastPosition.SpeedKmh,
                    trip.LastPosition.Source.ToString()
                ),
            trip.Roster.Count,
            trip.UnmarkedCount
        );
}
