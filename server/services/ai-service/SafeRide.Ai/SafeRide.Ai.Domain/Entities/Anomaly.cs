using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Domain.Entities;

public sealed class Anomaly
{
    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid TripId { get; private set; }
    public Guid BusId { get; private set; }
    public string RouteCode { get; private set; } = string.Empty;
    public string RouteName { get; private set; } = string.Empty;

    public AnomalyType Type { get; private set; }
    public AnomalyStatus Status { get; private set; }

    /// The event payload that triggered this, kept whole. The model reads it, and
    /// it is the only record of what was actually known at the time.
    public string ContextJson { get; private set; } = "{}";

    public DateTime DetectedAtUtc { get; private set; }

    public string? Classification { get; private set; }
    public string? Reasoning { get; private set; }
    public string? DraftMessage { get; private set; }
    public double? Confidence { get; private set; }
    public bool ClassifiedByModel { get; private set; }
    public DateTime? ClassifiedAtUtc { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    private Anomaly() { }

    public static Anomaly Detect(
        Guid schoolId,
        Guid tripId,
        Guid busId,
        string routeCode,
        string routeName,
        AnomalyType type,
        string contextJson,
        DateTime detectedAtUtc
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TripId = tripId,
            BusId = busId,
            RouteCode = routeCode,
            RouteName = routeName,
            Type = type,
            Status = AnomalyStatus.Detected,
            ContextJson = contextJson,
            DetectedAtUtc = detectedAtUtc,
        };

    public bool TryClassify(
        string classification,
        string reasoning,
        string draftMessage,
        double? confidence,
        bool byModel
    )
    {
        if (Status != AnomalyStatus.Detected)
        {
            return false;
        }

        Classification = classification;
        Reasoning = reasoning;
        DraftMessage = draftMessage;
        Confidence = confidence;
        ClassifiedByModel = byModel;
        ClassifiedAtUtc = DateTime.UtcNow;
        Status = AnomalyStatus.Classified;
        return true;
    }

    public bool TryApprove(Guid userId) => TryResolve(AnomalyStatus.Approved, userId);

    public bool TryDismiss(Guid userId) => TryResolve(AnomalyStatus.Dismissed, userId);

    private bool TryResolve(AnomalyStatus status, Guid userId)
    {
        if (Status != AnomalyStatus.Classified)
        {
            return false;
        }

        Status = status;
        ResolvedByUserId = userId;
        ResolvedAtUtc = DateTime.UtcNow;
        return true;
    }
}
