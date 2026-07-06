using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPresenceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisconnected",
                table: "ExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeatAt",
                table: "ExamAttempts",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PresenceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "DATETIME2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresenceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresenceLogs_ExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresenceLogs_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresenceLogs_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresenceLogs_AttemptId",
                table: "PresenceLogs",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_PresenceLogs_AttemptId_OccurredAt",
                table: "PresenceLogs",
                columns: new[] { "AttemptId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PresenceLogs_ExamId",
                table: "PresenceLogs",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_PresenceLogs_StudentId",
                table: "PresenceLogs",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresenceLogs");

            migrationBuilder.DropColumn(
                name: "IsDisconnected",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "LastHeartbeatAt",
                table: "ExamAttempts");
        }
    }
}
