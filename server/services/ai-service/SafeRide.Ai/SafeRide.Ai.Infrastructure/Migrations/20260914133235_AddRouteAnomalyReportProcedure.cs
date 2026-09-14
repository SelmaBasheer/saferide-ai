using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Ai.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteAnomalyReportProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE usp_RouteAnomalyReport
                    @SchoolId UNIQUEIDENTIFIER,
                    @FromUtc  DATETIME2,
                    @ToUtc    DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        a.RouteCode,
                        a.RouteName,
                        a.Type,
                        a.Classification,
                        COUNT(*) AS Total
                    FROM Anomalies a
                    WHERE a.SchoolId = @SchoolId
                      AND a.DetectedAtUtc >= @FromUtc
                      AND a.DetectedAtUtc <  @ToUtc
                    GROUP BY a.RouteCode, a.RouteName, a.Type, a.Classification
                    ORDER BY a.RouteCode, a.Type;
                END
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_RouteAnomalyReport;");
        }
    }
}
