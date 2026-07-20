using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVpnOrProxy = table.Column<bool>(type: "bit", nullable: false),
                    IsDuplicateSession = table.Column<bool>(type: "bit", nullable: false),
                    IsNetworkChangeDuringExam = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpAuditLogs_ExamAttempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IpAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditLogs_AttemptId_OccurredAt",
                table: "IpAuditLogs",
                columns: new[] { "ExamAttemptId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditLogs_IpAddress",
                table: "IpAuditLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_IpAuditLogs_UserId_OccurredAt",
                table: "IpAuditLogs",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpAuditLogs");
        }
    }
}
