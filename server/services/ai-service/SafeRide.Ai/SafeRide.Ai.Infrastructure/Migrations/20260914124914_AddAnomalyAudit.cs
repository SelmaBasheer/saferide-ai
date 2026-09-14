using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Ai.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnomalyAudits",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnomalyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyAudits", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyAudits_AnomalyId_ChangedAtUtc",
                table: "AnomalyAudits",
                columns: new[] { "AnomalyId", "ChangedAtUtc" }
            );

            migrationBuilder.Sql(
                """
                CREATE TRIGGER TR_Anomalies_StatusAudit
                ON Anomalies
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Set-based on purpose. A trigger fires once per statement,
                    -- not once per row, so anything written row-by-row here would
                    -- silently miss rows in a multi-row update.
                    INSERT INTO AnomalyAudits
                        (AnomalyId, SchoolId, OldStatus, NewStatus, ChangedByUserId, ChangedAtUtc)
                    SELECT
                        i.Id, i.SchoolId, d.Status, i.Status, i.ResolvedByUserId, SYSUTCDATETIME()
                    FROM inserted i
                    JOIN deleted d ON d.Id = i.Id
                    WHERE i.Status <> d.Status;
                END
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Anomalies_StatusAudit;");

            migrationBuilder.DropTable(name: "AnomalyAudits");
        }
    }
}
