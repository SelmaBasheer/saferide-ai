namespace SafeRide.Analytics.Models;

public sealed record ReportRange(DateOnly From, DateOnly To);

public sealed record SuperAdminSummary(
    int TotalSchools,
    int ApprovedSchools,
    int SuspendedSchools,
    long RevenuePaise,
    int PaymentCount,
    int FailedPaymentCount
);

public sealed record SchoolReportRow(
    string Name,
    string? City,
    string Status,
    string? PlanName,
    string SubscriptionStatus,
    DateOnly? SubscriptionEndsOn,
    int? BusLimit,
    int BusesInUse
);

public sealed record RevenueReportRow(
    DateOnly PaymentDate,
    string SchoolName,
    string PlanName,
    long AmountPaise,
    string Status
);

public sealed record PlanReportRow(string PlanName, int PaymentCount, long RevenuePaise);

public sealed record SuperAdminReport(
    ReportRange Range,
    SuperAdminSummary Summary,
    IReadOnlyList<SchoolReportRow> Schools,
    IReadOnlyList<RevenueReportRow> Revenue,
    IReadOnlyList<PlanReportRow> Plans
);
