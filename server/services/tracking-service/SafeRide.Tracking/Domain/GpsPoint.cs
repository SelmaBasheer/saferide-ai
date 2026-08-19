using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace SafeRide.Tracking.Domain;

public sealed class GpsPoint
{
    [BsonId]
    public ObjectId Id { get; set; }

    public Guid TripId { get; init; }
    public Guid SchoolId { get; init; }
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; init; } = default!;
    public DateTime RecordedAt { get; init; }
    public double? SpeedKmh { get; init; }

    [BsonRepresentation(BsonType.String)]
    public PositionSource Source { get; init; }
}
