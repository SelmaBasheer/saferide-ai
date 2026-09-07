using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtpAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "OtpCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Attempts", table: "OtpCodes");
        }
    }
}
