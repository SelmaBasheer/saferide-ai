using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SafeRide.Tracking.Domain;

public sealed class RosterEntry
{
    public Guid StudentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ParentEmail { get; init; } = string.Empty;
    public Guid PickupStopId { get; init; }

    [BsonRepresentation(BsonType.String)]
    public BoardingStatus BoardingStatus { get; set; } = BoardingStatus.Unmarked;
    public DateTime? MarkedAt { get; set; }
    public Guid? MarkedBy { get; set; }
}
