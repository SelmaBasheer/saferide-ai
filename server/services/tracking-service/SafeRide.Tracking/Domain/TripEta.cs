namespace SafeRide.Tracking.Domain;

/// <summary>
/// Estimates arrival times from the trip's own history rather than a road model.
/// The bus's average speed so far already contains the traffic, the junctions and
/// the time spent boarding at each stop — a generic routing estimate contains none
/// of them.
/// </summary>
public static class TripEta
{
    private const double MinSpeedKmh = 5;
    private const double MaxSpeedKmh = 60;

    // Below this, the average is dominated by noise and the estimate is worthless.
    private const double MinMetresBeforeEstimating = 300;

    public static Dictionary<Guid, DateTime> ForTrip(Trip trip)
    {
        var etas = new Dictionary<Guid, DateTime>();

        if (
            trip.Status != TripStatus.Active
            || trip.LastPosition is null
            || trip.Route.Path is null
        )
        {
            return etas;
        }

        var path = trip
            .Route.Path.Coordinates.Positions.Select(p => (p.Latitude, p.Longitude))
            .ToList();

        if (path.Count < 2)
        {
            return etas;
        }

        var cumulative = PathProgress.Cumulative(path);

        var busAlong = PathProgress.DistanceAlong(
            trip.LastPosition.Location.Latitude(),
            trip.LastPosition.Location.Longitude(),
            path,
            cumulative
        );

        var elapsedSeconds = (trip.LastPosition.RecordedAt - trip.StartedAt).TotalSeconds;

        if (busAlong < MinMetresBeforeEstimating || elapsedSeconds <= 0)
        {
            return etas;
        }

        var speedKmh = Math.Clamp(busAlong / elapsedSeconds * 3.6, MinSpeedKmh, MaxSpeedKmh);
        var metresPerSecond = speedKmh / 3.6;

        foreach (var stop in trip.Route.Stops.Where(s => s.ReachedAt is null))
        {
            var remaining = stop.DistanceFromStartMetres - busAlong;

            if (remaining <= 0)
            {
                continue; // bus is already past this stop's position on the path
            }

            etas[stop.StopId] = trip.LastPosition.RecordedAt.AddSeconds(
                remaining / metresPerSecond
            );
        }

        return etas;
    }
}
