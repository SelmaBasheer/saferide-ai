using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SafeRide.Tracking.Common;

namespace SafeRide.Tracking.Domain;

public sealed class Trip
{
    [BsonId]
    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }
    public Guid RouteId { get; private set; }
    public Guid BusId { get; private set; }
    public Guid DriverId { get; private set; }

    [BsonRepresentation(BsonType.String)]
    public TripStatus Status { get; private set; }

    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    public TripRoute Route { get; private set; } = default!;
    public List<RosterEntry> Roster { get; private set; } = [];
    public LastPosition? LastPosition { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public DateTime? DeviationAlertedAt { get; private set; }

    private Trip() { }

    public static Trip Start(
        Guid schoolId,
        Guid routeId,
        Guid busId,
        Guid driverId,
        TripRoute route,
        List<RosterEntry> roster
    )
    {
        var now = DateTime.UtcNow;

        return new Trip
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            RouteId = routeId,
            BusId = busId,
            DriverId = driverId,
            Status = TripStatus.Active,
            StartedAt = now,
            Route = route,
            Roster = roster,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void RecordPosition(
        double latitude,
        double longitude,
        double? speedKmh,
        PositionSource source
    )
    {
        RequireActive();

        LastPosition = new LastPosition
        {
            Location = Geo.Point(latitude, longitude),
            RecordedAt = DateTime.UtcNow,
            SpeedKmh = speedKmh,
            Source = source,
        };

        UpdatedAt = DateTime.UtcNow;
    }

    public bool TryMarkStopReached(Guid stopId, out TripStop? stop)
    {
        RequireActive();

        stop = Route.Stops.FirstOrDefault(s => s.StopId == stopId);
        if (stop is null || stop.ReachedAt is not null)
        {
            return false;
        }

        stop.ReachedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public RosterEntry MarkBoarding(Guid studentId, BoardingStatus status, Guid markedBy)
    {
        RequireActive();

        if (status == BoardingStatus.Unmarked)
        {
            throw AppException.Validation("A student cannot be set back to unmarked.");
        }

        var entry =
            Roster.FirstOrDefault(r => r.StudentId == studentId)
            ?? throw AppException.NotFound("Student is not on this trip.");

        var stop =
            Route.Stops.FirstOrDefault(s => s.StopId == entry.PickupStopId)
            ?? throw AppException.Validation("This student's stop is not on this route.");

        if (stop.ReachedAt is null)
        {
            throw AppException.Conflict($"The bus has not reached {stop.Name} yet.");
        }

        entry.BoardingStatus = status;
        entry.MarkedAt = DateTime.UtcNow;
        entry.MarkedBy = markedBy;
        UpdatedAt = DateTime.UtcNow;

        return entry;
    }

    public void End()
    {
        RequireActive();
        Status = TripStatus.Completed;
        EndedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        RequireActive();
        Status = TripStatus.Cancelled;
        EndedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int UnmarkedCount => Roster.Count(r => r.BoardingStatus == BoardingStatus.Unmarked);

    public IEnumerable<RosterEntry> StudentsAtStop(Guid stopId) =>
        Roster.Where(r => r.PickupStopId == stopId);

    public TripStop? StopAhead(int currentSequence, int offset) =>
        Route.Stops.FirstOrDefault(s => s.Sequence == currentSequence + offset);

    private void RequireActive()
    {
        if (Status != TripStatus.Active)
        {
            throw AppException.Conflict(
                $"Trip is {Status.ToString().ToLowerInvariant()}, not active."
            );
        }
    }

    public bool ShouldAlertDeviation(TimeSpan cooldown) =>
        Status == TripStatus.Active
        && (DeviationAlertedAt is null || DateTime.UtcNow - DeviationAlertedAt > cooldown);

    public void MarkDeviationAlerted()
    {
        DeviationAlertedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
