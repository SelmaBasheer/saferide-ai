using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Services;

public static class SuperAdminReportPdf
{
    private static readonly string[] SchoolHeaders =
    [
        "School",
        "City",
        "Status",
        "Plan",
        "Subscription",
        "Ends",
        "Buses",
    ];

    private static readonly string[] RevenueHeaders =
    [
        "Date",
        "School",
        "Plan",
        "Amount (INR)",
        "Status",
    ];

    public static byte[] Render(SuperAdminReport report) =>
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

    private static void Header(IContainer container, SuperAdminReport report)
    {
        container
            .PaddingBottom(12)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Column(column =>
            {
                column.Item().Text("SafeRide AI — Platform report").FontSize(16).SemiBold();

                column
                    .Item()
                    .PaddingTop(2)
                    .Text(
                        $"{report.Range.From:dd MMM yyyy} to {report.Range.To:dd MMM yyyy}  ·  "
                            + $"generated {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC"
                    )
                    .FontSize(9)
                    .FontColor(Colors.Grey.Medium);
            });
    }

    private static void Content(IContainer container, SuperAdminReport report)
    {
        container
            .PaddingVertical(14)
            .Column(column =>
            {
                column.Spacing(16);

                column.Item().Element(c => Summary(c, report.Summary));
                column.Item().Element(c => Schools(c, report.Schools));
                column.Item().Element(c => Revenue(c, report.Revenue));
                column.Item().Element(c => Plans(c, report.Plans));

                column
                    .Item()
                    .PaddingTop(6)
                    .Text(
                        "Student and trip data is deliberately absent: a platform administrator "
                            + "has no need to see a child's record."
                    )
                    .FontSize(8)
                    .Italic()
                    .FontColor(Colors.Grey.Medium);
            });
    }

    private static void Summary(IContainer container, SuperAdminSummary summary)
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

            Card("Schools", summary.TotalSchools.ToString());
            Card("Approved", summary.ApprovedSchools.ToString());
            Card("Suspended", summary.SuspendedSchools.ToString());
            Card("Revenue", Rupees(summary.RevenuePaise));
            Card("Payments", summary.PaymentCount.ToString());
        });
    }

    private static void Schools(IContainer container, IReadOnlyList<SchoolReportRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Schools");

            if (rows.Count == 0)
            {
                column.Item().Text("No schools.").FontColor(Colors.Grey.Medium);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in SchoolHeaders)
                        {
                            header.Cell().Element(HeaderCell).Text(title);
                        }
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(BodyCell).Text(r.Name);
                        table.Cell().Element(BodyCell).Text(r.City ?? "—");
                        table.Cell().Element(BodyCell).Text(r.Status);
                        table.Cell().Element(BodyCell).Text(r.PlanName ?? "—");
                        table.Cell().Element(BodyCell).Text(r.SubscriptionStatus);
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(r.SubscriptionEndsOn?.ToString("dd MMM yyyy") ?? "—");
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text($"{r.BusesInUse} / {r.BusLimit?.ToString() ?? "∞"}");
                    }
                });
        });
    }

    private static void Revenue(IContainer container, IReadOnlyList<RevenueReportRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Revenue");

            if (rows.Count == 0)
            {
                column.Item().Text("No payments in this range.").FontColor(Colors.Grey.Medium);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in RevenueHeaders)
                        {
                            header.Cell().Element(HeaderCell).Text(title);
                        }
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(BodyCell).Text(r.PaymentDate.ToString("dd MMM yyyy"));
                        table.Cell().Element(BodyCell).Text(r.SchoolName);
                        table.Cell().Element(BodyCell).Text(r.PlanName);
                        table.Cell().Element(BodyCell).AlignRight().Text(Rupees(r.AmountPaise));
                        table.Cell().Element(BodyCell).Text(r.Status);
                    }
                });
        });
    }

    private static void Plans(IContainer container, IReadOnlyList<PlanReportRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("By plan");

            if (rows.Count == 0)
            {
                column.Item().Text("Nothing sold in this range.").FontColor(Colors.Grey.Medium);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Plan");
                        header.Cell().Element(HeaderCell).Text("Payments");
                        header.Cell().Element(HeaderCell).Text("Revenue (INR)");
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(BodyCell).Text(r.PlanName);
                        table.Cell().Element(BodyCell).Text(r.PaymentCount.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(Rupees(r.RevenuePaise));
                    }
                });
        });
    }

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

    /// <summary>
    /// Deliberately no ₹ symbol. QuestPDF's default font has no rupee glyph and
    /// would render a hollow box on every amount — the column header says INR
    /// instead, which is unambiguous and always prints.
    /// </summary>
    private static string Rupees(long paise) =>
        (paise / 100m).ToString("N0", CultureInfo.InvariantCulture);
}
