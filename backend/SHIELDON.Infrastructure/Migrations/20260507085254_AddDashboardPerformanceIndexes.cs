using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId_Status",
                table: "ExamAttempts",
                columns: new[] { "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId_Status",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_ExamId_Status",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId_Status",
                table: "CourseEnrollments");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments",
                column: "CourseId");
        }
    }
}
