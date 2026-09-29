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
}
