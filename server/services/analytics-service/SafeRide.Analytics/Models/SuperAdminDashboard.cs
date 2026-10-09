namespace SafeRide.Analytics.Models;

public sealed record SubscriptionCounts(
    int Active,
    int ExpiringSoon,
    int InGrace,
    int Expired,
    int Cancelled
);

public sealed record SchoolCounts(
    int Onboarded,
    int Approved,
    int Suspended,
    int WithExpiredSubscription,
    int InRangeOnboarded,
    int InRangeApproved,
    int InRangeSuspended
);

public sealed record RevenueTotals(
    long TotalPaise,
    long ThisMonthPaise,
    long ThisYearPaise,
    int PaymentCount,
    long InRangePaise,
    int InRangeCount
);

public sealed record MonthlyRevenue(DateOnly Month, long RevenuePaise, int PaymentCount);

public sealed record RecentSchool(
    string Name,
    string? City,
    string? PlanName,
    DateTime OnboardedAtUtc,
    string Status
);

public sealed record RecentPurchase(
    DateOnly PaymentDate,
    string SchoolName,
    string PlanName,
    long AmountPaise,
    string Status
);

public sealed record SuperAdminDashboard(
    ReportRange Range,
    SubscriptionCounts Subscriptions,
    SchoolCounts Schools,
    RevenueTotals Revenue,
    IReadOnlyList<MonthlyRevenue> RevenueByMonth,
    IReadOnlyList<PlanReportRow> Plans,
    IReadOnlyList<RecentSchool> RecentSchools,
    IReadOnlyList<RecentPurchase> RecentPurchases
);
