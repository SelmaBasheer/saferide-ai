namespace SafeRide.Tracking.Domain;

/// <summary>
/// Turns a path into a one-dimensional ruler. Every point on the route becomes a
/// distance from the start, so "where is the bus" and "where is the stop" can be
/// subtracted instead of compared geometrically.
/// </summary>
public static class PathProgress
{
    /// Distance from the start of the path to each point, in metres.
    public static double[] Cumulative(IReadOnlyList<(double Lat, double Lon)> path)
    {
        var distances = new double[path.Count];

        for (var i = 1; i < path.Count; i++)
        {
            distances[i] =
                distances[i - 1]
                + GeoDistance.Metres(path[i - 1].Lat, path[i - 1].Lon, path[i].Lat, path[i].Lon);
        }

        return distances;
    }

    /// How far along the path the given position sits, in metres. Projects onto the
    /// nearest segment, so a bus a few metres off the centre line still maps sensibly.
    public static double DistanceAlong(
        double latitude,
        double longitude,
        IReadOnlyList<(double Lat, double Lon)> path,
        double[] cumulative
    )
    {
        if (path.Count < 2)
        {
            return 0;
        }

        var bestOffset = double.MaxValue;
        var bestAlong = 0d;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var (offset, along) = ProjectOntoSegment(latitude, longitude, path[i], path[i + 1]);

            if (offset < bestOffset)
            {
                bestOffset = offset;
                bestAlong = cumulative[i] + along;
            }
        }

        return bestAlong;
    }

    private static (double Offset, double Along) ProjectOntoSegment(
        double lat,
        double lon,
        (double Lat, double Lon) a,
        (double Lat, double Lon) b
    )
    {
        // local flat projection — accurate over the few hundred metres a segment spans
        const double LatScale = 111_320.0;
        var lonScale = 111_320.0 * Math.Cos(lat * Math.PI / 180);

        var px = (lon - a.Lon) * lonScale;
        var py = (lat - a.Lat) * LatScale;
        var bx = (b.Lon - a.Lon) * lonScale;
        var by = (b.Lat - a.Lat) * LatScale;

        var lengthSquared = bx * bx + by * by;
        var t = lengthSquared == 0 ? 0 : Math.Clamp((px * bx + py * by) / lengthSquared, 0, 1);

        var dx = px - t * bx;
        var dy = py - t * by;

        return (Math.Sqrt(dx * dx + dy * dy), t * Math.Sqrt(lengthSquared));
    }
}
