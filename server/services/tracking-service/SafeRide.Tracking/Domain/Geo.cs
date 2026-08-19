using MongoDB.Driver.GeoJsonObjectModel;

namespace SafeRide.Tracking.Domain;

public static class Geo
{
    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> Point(
        double latitude,
        double longitude
    ) => GeoJson.Point(GeoJson.Geographic(longitude, latitude));

    public static double Latitude(this GeoJsonPoint<GeoJson2DGeographicCoordinates> point) =>
        point.Coordinates.Latitude;

    public static double Longitude(this GeoJsonPoint<GeoJson2DGeographicCoordinates> point) =>
        point.Coordinates.Longitude;

    public static GeoJsonLineString<GeoJson2DGeographicCoordinates> Line(
        IEnumerable<(double Latitude, double Longitude)> points
    ) =>
        GeoJson.LineString(
            points.Select(p => GeoJson.Geographic(p.Longitude, p.Latitude)).ToArray()
        );
}
