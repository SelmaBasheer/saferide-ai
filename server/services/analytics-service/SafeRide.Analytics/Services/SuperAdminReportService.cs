using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Common;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Services;

public sealed class SuperAdminReportService(
    ISuperAdminReportRepository repository,
    LocalDates localDates
)
{
    /// A year, not ninety days. Revenue is naturally asked for annually, and a
    /// cap exists only to stop an unbounded scan — from=1990 would pull every
    /// payment ever into memory.
    private const int MaxRangeDays = 366;

    private const int DefaultRangeDays = 30;

    public ReportRange ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = localDates.ToLocalDate(DateTime.UtcNow);

        var end = to ?? today;
        var start = from ?? end.AddDays(-DefaultRangeDays);

        if (start > end)
        {
            throw AppException.BadRequest(
                "Report.InvalidRange",
                "The start date must be on or before the end date."
            );
        }

        if (end.DayNumber - start.DayNumber > MaxRangeDays)
        {
            throw AppException.BadRequest(
                "Report.RangeTooLarge",
                $"The range cannot be longer than {MaxRangeDays} days."
            );
        }

        return new ReportRange(start, end);
    }

    public Task<SuperAdminReport> BuildAsync(ReportRange range, CancellationToken ct) =>
        repository.BuildAsync(range, ct);

    public static byte[] ToCsv(SuperAdminReport report)
    {
        var csv = new CsvBuilder();

        csv.Section("SafeRide AI — Platform report")
            .Row("Range", $"{report.Range.From:yyyy-MM-dd} to {report.Range.To:yyyy-MM-dd}")
            .Row("Generated", DateTime.UtcNow);

        csv.Section("Summary")
            .Row("Total schools", report.Summary.TotalSchools)
            .Row("Approved", report.Summary.ApprovedSchools)
            .Row("Suspended", report.Summary.SuspendedSchools)
            .Row("Revenue (INR)", Rupees(report.Summary.RevenuePaise))
            .Row("Payments", report.Summary.PaymentCount)
            .Row("Failed payments", report.Summary.FailedPaymentCount);

        csv.Section("Schools")
            .Row(
                "Name",
                "City",
                "Status",
                "Plan",
                "Subscription",
                "Ends on",
                "Buses used",
                "Bus limit"
            );

        foreach (var s in report.Schools)
        {
            csv.Row(
                s.Name,
                s.City,
                s.Status,
                s.PlanName,
                s.SubscriptionStatus,
                s.SubscriptionEndsOn,
                s.BusesInUse,
                // Null means unlimited, and an empty cell would read as zero.
                s.BusLimit?.ToString() ?? "Unlimited"
            );
        }

        csv.Section("Revenue").Row("Date", "School", "Plan", "Amount (INR)", "Status");

        foreach (var r in report.Revenue)
        {
            csv.Row(r.PaymentDate, r.SchoolName, r.PlanName, Rupees(r.AmountPaise), r.Status);
        }

        csv.Section("By plan").Row("Plan", "Payments", "Revenue (INR)");

        foreach (var p in report.Plans)
        {
            csv.Row(p.PlanName, p.PaymentCount, Rupees(p.RevenuePaise));
        }

        return csv.ToBytes();
    }

    /// Written as a number rather than "₹14,999" so Excel can sum the column.
    /// Paise are integers everywhere else; this is the only place they become
    /// rupees, and it is the last step before the file is written.
    private static decimal Rupees(long paise) => paise / 100m;
}
