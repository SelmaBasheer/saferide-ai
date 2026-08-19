using MongoDB.Driver.GeoJsonObjectModel;

namespace SafeRide.Tracking.Domain;

//Copied from Route at trip start, never refreshed
//Yesterday's trip shows yesterday's stops even after today's edits — and a bus in motion keeps working if Route is deploying.
public sealed class TripRoute
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<TripStop> Stops { get; init; } = [];
    public GeoJsonLineString<GeoJson2DGeographicCoordinates>? Path { get; init; }
}
