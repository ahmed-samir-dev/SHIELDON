using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDeleteAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseDeleteAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDeleteAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDeleteAuditLogs_Users_DeletedByAdminId",
                        column: x => x.DeletedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDeleteAuditLogs_DeletedAt",
                table: "CourseDeleteAuditLogs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDeleteAuditLogs_DeletedByAdminId",
                table: "CourseDeleteAuditLogs",
                column: "DeletedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseDeleteAuditLogs");
        }
    }
}
