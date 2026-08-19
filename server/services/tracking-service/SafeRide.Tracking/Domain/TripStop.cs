using MongoDB.Driver.GeoJsonObjectModel;

namespace SafeRide.Tracking.Domain;

public sealed class TripStop
{
    public Guid StopId { get; init; }
    public int Sequence { get; init; }
    public string Name { get; init; } = string.Empty;
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; init; } = default!;
    public string PickupTime { get; init; } = string.Empty;
    public DateTime? ReachedAt { get; set; }
}
