using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRide.Schools.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSchoolDocumentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SchoolDocument_Schools_SchoolId",
                table: "SchoolDocument"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_SchoolDocument", table: "SchoolDocument");

            migrationBuilder.RenameTable(name: "SchoolDocument", newName: "SchoolDocuments");

            migrationBuilder.RenameIndex(
                name: "IX_SchoolDocument_SchoolId_Type",
                table: "SchoolDocuments",
                newName: "IX_SchoolDocuments_SchoolId_Type"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_SchoolDocuments",
                table: "SchoolDocuments",
                column: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_SchoolDocuments_Schools_SchoolId",
                table: "SchoolDocuments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SchoolDocuments_Schools_SchoolId",
                table: "SchoolDocuments"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_SchoolDocuments", table: "SchoolDocuments");

            migrationBuilder.RenameTable(name: "SchoolDocuments", newName: "SchoolDocument");

            migrationBuilder.RenameIndex(
                name: "IX_SchoolDocuments_SchoolId_Type",
                table: "SchoolDocument",
                newName: "IX_SchoolDocument_SchoolId_Type"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_SchoolDocument",
                table: "SchoolDocument",
                column: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_SchoolDocument_Schools_SchoolId",
                table: "SchoolDocument",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
