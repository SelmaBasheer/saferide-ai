using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Services;

public static class SchoolReportPdf
{
    // Green for boarded, red for absent, grey for unmarked. Unmarked is not a
    // third outcome — it is the absence of one, and the colour should say so.
    private static readonly string BoardedColour = Colors.Green.Darken1;
    private static readonly string AbsentColour = Colors.Red.Darken1;
    private static readonly string UnmarkedColour = Colors.Grey.Lighten1;

    private static readonly string[] AttendanceHeaders =
    [
        "Date",
        "Route",
        "Student",
        "Stop",
        "Status",
        "Marked",
    ];

    private static readonly string[] TripHeaders =
    [
        "Date",
        "Route",
        "Bus",
        "Started",
        "Duration",
        "Boarded",
        "Absent",
        "Unmarked",
    ];

    // A report is a document, not a user interface. Every number and date is
    // formatted invariantly so the same range produces the same file on any
    // machine — otherwise the server's locale leaks into the output.
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static byte[] Render(SchoolReport report) =>
        Document
            .Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(c => Header(c, report));
                    page.Content().Element(c => Content(c, report));

                    page.Footer()
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                            t.Span("Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                });
            })
            .GeneratePdf();

    private static void Header(IContainer container, SchoolReport report)
    {
        var title =
            report.Section == SchoolReportSection.Attendance ? "Attendance report" : "Trip history";

        container
            .PaddingBottom(12)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Column(column =>
            {
                column.Item().Text($"SafeRide AI — {title}").FontSize(16).SemiBold();

                var from = report.Range.From.ToString("dd MMM yyyy", Culture);
                var to = report.Range.To.ToString("dd MMM yyyy", Culture);
                var generated = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm", Culture);

                column
                    .Item()
                    .PaddingTop(2)
                    .Text($"{from} to {to}  ·  generated {generated} UTC")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Medium);
            });
    }

    private static void Content(IContainer container, SchoolReport report)
    {
        container
            .PaddingVertical(14)
            .Column(column =>
            {
                column.Spacing(16);

                column.Item().Element(c => Summary(c, report.Summary));
                column.Item().Element(c => Breakdown(c, report.Summary));

                if (report.Section == SchoolReportSection.Attendance)
                {
                    column.Item().Element(c => Attendance(c, report.Attendance));
                }
                else
                {
                    column.Item().Element(c => Trips(c, report.Trips));
                }

                column
                    .Item()
                    .PaddingTop(6)
                    .Text(
                        "The attendance rate counts boarded against absent only. Unmarked "
                            + "students are excluded rather than assumed present."
                    )
                    .FontSize(8)
                    .Italic()
                    .FontColor(Colors.Grey.Medium);
            });
    }

    private static void Summary(IContainer container, SchoolReportSummary summary)
    {
        container.Row(row =>
        {
            row.Spacing(10);

            void Card(string label, string value)
            {
                row.RelativeItem()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Medium);
                        c.Item().PaddingTop(2).Text(value).FontSize(13).SemiBold();
                    });
            }

            Card("Trips", summary.Trips.ToString(Culture));
            Card("Boarded", summary.Boarded.ToString(Culture));
            Card("Absent", summary.Absent.ToString(Culture));
            Card("Unmarked", summary.Unmarked.ToString(Culture));
            Card("Attendance", $"{summary.AttendanceRate.ToString(Culture)}%");
        });
    }

    /// <summary>
    /// A stacked bar rather than a pie: it reads at a glance on paper, needs no
    /// legend beside it, and costs three coloured rectangles instead of an
    /// image. Segments with a zero value are omitted — a zero-width relative
    /// item has no meaning.
    /// </summary>
    private static void Breakdown(IContainer container, SchoolReportSummary summary)
    {
        var total = summary.Boarded + summary.Absent + summary.Unmarked;

        if (total == 0)
        {
            container.Text("No boarding records in this range.").FontColor(Colors.Grey.Medium);
            return;
        }

        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Boarding breakdown");

            column
                .Item()
                .Height(14)
                .Row(row =>
                {
                    if (summary.Boarded > 0)
                        row.RelativeItem(summary.Boarded).Background(BoardedColour);

                    if (summary.Absent > 0)
                        row.RelativeItem(summary.Absent).Background(AbsentColour);

                    if (summary.Unmarked > 0)
                        row.RelativeItem(summary.Unmarked).Background(UnmarkedColour);
                });

            column
                .Item()
                .PaddingTop(4)
                .Text(
                    $"Boarded {summary.Boarded.ToString(Culture)}  ·  "
                        + $"Absent {summary.Absent.ToString(Culture)}  ·  "
                        + $"Unmarked {summary.Unmarked.ToString(Culture)}"
                )
                .FontSize(8)
                .FontColor(Colors.Grey.Medium);
        });
    }

    private static void Attendance(IContainer container, IReadOnlyList<AttendanceReportRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Attendance");

            if (rows.Count == 0)
            {
                column
                    .Item()
                    .Text("No completed trips in this range.")
                    .FontColor(Colors.Grey.Medium);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(4);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in AttendanceHeaders)
                        {
                            header.Cell().Element(HeaderCell).Text(title);
                        }
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(BodyCell).Text(r.TripDate.ToString("dd MMM", Culture));
                        table.Cell().Element(BodyCell).Text(r.RouteCode ?? "—");
                        table.Cell().Element(BodyCell).Text(r.StudentName);
                        table.Cell().Element(BodyCell).Text(r.StopName ?? "—");
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(r.Status)
                            .FontColor(StatusColour(r.Status));
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(r.MarkedAtUtc?.ToString("HH:mm", Culture) ?? "—");
                    }
                });
        });
    }

    private static void Trips(IContainer container, IReadOnlyList<TripReportRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Trip history");

            if (rows.Count == 0)
            {
                column
                    .Item()
                    .Text("No completed trips in this range.")
                    .FontColor(Colors.Grey.Medium);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in TripHeaders)
                        {
                            header.Cell().Element(HeaderCell).Text(title);
                        }
                    });

                    foreach (var t in rows)
                    {
                        table.Cell().Element(BodyCell).Text(t.TripDate.ToString("dd MMM", Culture));
                        table.Cell().Element(BodyCell).Text(t.RouteCode ?? "—");
                        table.Cell().Element(BodyCell).Text(t.BusRegistration ?? "—");
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(t.StartedAtUtc.ToString("HH:mm", Culture));
                        table.Cell().Element(BodyCell).Text(Duration(t.StartedAtUtc, t.EndedAtUtc));
                        table.Cell().Element(BodyCell).Text(t.BoardedCount.ToString(Culture));
                        table.Cell().Element(BodyCell).Text(t.AbsentCount.ToString(Culture));
                        table.Cell().Element(BodyCell).Text(t.UnmarkedCount.ToString(Culture));
                    }
                });
        });
    }

    private static string Duration(DateTime start, DateTime? end)
    {
        if (end is null)
            return "—";

        var minutes = (int)Math.Round((end.Value - start).TotalMinutes);

        return minutes < 60
            ? $"{minutes.ToString(Culture)}m"
            : $"{(minutes / 60).ToString(Culture)}h {(minutes % 60).ToString(Culture)}m";
    }

    private static string StatusColour(string status) =>
        status switch
        {
            "Boarded" => BoardedColour,
            "Absent" => AbsentColour,
            _ => Colors.Grey.Medium,
        };

    private static IContainer SectionTitle(IContainer container) =>
        container.PaddingBottom(4).DefaultTextStyle(t => t.FontSize(11).SemiBold());

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(t => t.FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2));

    private static IContainer BodyCell(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .PaddingHorizontal(4);
}
