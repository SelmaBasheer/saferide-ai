namespace SafeRide.Tracking.Domain;

public static class GeoDistance
{
    private const double EarthRadiusMetres = 6_371_000;

    public static double Metres(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRadians(lat1))
                * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2)
                * Math.Sin(dLon / 2);

        return EarthRadiusMetres * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    public static double MetresToPath(
        double latitude,
        double longitude,
        IReadOnlyList<(double Lat, double Lon)> path
    )
    {
        if (path.Count < 2)
        {
            return double.MaxValue;
        }

        var best = double.MaxValue;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var d = MetresToSegment(latitude, longitude, path[i], path[i + 1]);
            if (d < best)
            {
                best = d;
            }
        }

        return best;
    }

    private static double MetresToSegment(
        double lat,
        double lon,
        (double Lat, double Lon) a,
        (double Lat, double Lon) b
    )
    {
        // local flat projection — accurate over the few hundred metres a segment spans
        var latScale = 111_320.0;
        var lonScale = 111_320.0 * Math.Cos(ToRadians(lat));

        var px = (lon - a.Lon) * lonScale;
        var py = (lat - a.Lat) * latScale;
        var bx = (b.Lon - a.Lon) * lonScale;
        var by = (b.Lat - a.Lat) * latScale;

        var lengthSquared = bx * bx + by * by;
        var t = lengthSquared == 0 ? 0 : Math.Clamp((px * bx + py * by) / lengthSquared, 0, 1);

        var dx = px - t * bx;
        var dy = py - t * by;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}
