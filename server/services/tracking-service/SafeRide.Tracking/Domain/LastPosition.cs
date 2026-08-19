using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace SafeRide.Tracking.Domain;

public sealed class LastPosition
{
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; init; } = default!;
    public DateTime RecordedAt { get; init; }
    public double? SpeedKmh { get; init; }

    [BsonRepresentation(BsonType.String)]
    public PositionSource Source { get; init; }
}
