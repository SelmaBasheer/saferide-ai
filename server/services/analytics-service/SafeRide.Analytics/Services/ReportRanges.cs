using SafeRide.Analytics.Common;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Services;

public static class ReportRanges
{
    /// A year, not ninety days. Both revenue and attendance are naturally asked
    /// for annually; the cap exists only to stop an unbounded scan.
    public const int MaxRangeDays = 366;

    private const int DefaultRangeDays = 30;

    public static ReportRange Resolve(DateOnly? from, DateOnly? to, DateOnly today)
    {
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

    /// <summary>
    /// The dashboard's default is a year rather than a month, because its charts
    /// are about trend: thirty days of bars shows noise, twelve months shows a
    /// shape. Starting at the first of the month keeps the first bar whole.
    /// </summary>
    public static ReportRange ResolveDashboard(DateOnly? from, DateOnly? to, DateOnly today)
    {
        if (from is null && to is null)
        {
            var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
            return new ReportRange(start, today);
        }

        return Resolve(from, to, today);
    }
}
