using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Schools.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToSchoolDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SchoolDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.Sql("UPDATE SchoolDocuments SET TenantId = SchoolId;");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolDocuments_TenantId",
                table: "SchoolDocuments",
                column: "TenantId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SchoolDocuments_TenantId",
                table: "SchoolDocuments"
            );

            migrationBuilder.DropColumn(name: "TenantId", table: "SchoolDocuments");
        }
    }
}
