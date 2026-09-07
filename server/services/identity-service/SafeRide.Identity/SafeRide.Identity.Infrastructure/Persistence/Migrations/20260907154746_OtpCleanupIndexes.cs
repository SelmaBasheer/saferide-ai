using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtpCleanupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_ExpiresAtUtc",
                table: "OtpCodes",
                column: "ExpiresAtUtc"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_OtpCodes_ExpiresAtUtc", table: "OtpCodes");
        }
    }
}
