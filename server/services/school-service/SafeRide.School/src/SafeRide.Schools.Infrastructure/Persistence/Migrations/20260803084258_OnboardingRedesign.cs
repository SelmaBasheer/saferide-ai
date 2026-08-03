using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Schools.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnboardingRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizedPersonDesignation",
                table: "Schools",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedPersonName",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Board",
                table: "Schools",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "BusCount",
                table: "Schools",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Schools",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "OfficialEmail",
                table: "Schools",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "OfficialPhone",
                table: "Schools",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Schools",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "Schools",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Schools",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Schools",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "StudentCount",
                table: "Schools",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "Schools",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "SchoolDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(
                        type: "nvarchar(260)",
                        maxLength: 260,
                        nullable: false
                    ),
                    BlobKey = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    ContentType = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolDocument_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocument_SchoolId_Type",
                table: "SchoolDocument",
                columns: new[] { "SchoolId", "Type" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SchoolDocument");

            migrationBuilder.DropColumn(name: "AuthorizedPersonDesignation", table: "Schools");

            migrationBuilder.DropColumn(name: "AuthorizedPersonName", table: "Schools");

            migrationBuilder.DropColumn(name: "Board", table: "Schools");

            migrationBuilder.DropColumn(name: "BusCount", table: "Schools");

            migrationBuilder.DropColumn(name: "LegalName", table: "Schools");

            migrationBuilder.DropColumn(name: "OfficialEmail", table: "Schools");

            migrationBuilder.DropColumn(name: "OfficialPhone", table: "Schools");

            migrationBuilder.DropColumn(name: "RegistrationNumber", table: "Schools");

            migrationBuilder.DropColumn(name: "RejectedAtUtc", table: "Schools");

            migrationBuilder.DropColumn(name: "RejectionReason", table: "Schools");

            migrationBuilder.DropColumn(name: "ReviewedByUserId", table: "Schools");

            migrationBuilder.DropColumn(name: "StudentCount", table: "Schools");

            migrationBuilder.DropColumn(name: "SubmittedAtUtc", table: "Schools");
        }
    }
}
