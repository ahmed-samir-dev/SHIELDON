using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CentralizedQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing questions to avoid FK conflict when migrating to course-scoped bank
            migrationBuilder.Sql("DELETE FROM AttemptAnswers;");
            migrationBuilder.Sql("DELETE FROM ExamAttempts;");
            migrationBuilder.Sql("DELETE FROM QuestionOptions;");
            migrationBuilder.Sql("DELETE FROM ExamQuestions;");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_Exams_ExamId",
                table: "ExamQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ExamQuestions_ExamId",
                table: "ExamQuestions");

            migrationBuilder.RenameColumn(
                name: "ExamId",
                table: "ExamQuestions",
                newName: "CreatedByUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "ExamQuestions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ExamQuestions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ExamAttemptQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttemptQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttemptQuestions_ExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAttemptQuestions_ExamQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSelectionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSelectionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSelectionRules_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_CourseId",
                table: "ExamQuestions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptQuestions_AttemptId",
                table: "ExamAttemptQuestions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptQuestions_QuestionId",
                table: "ExamAttemptQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSelectionRules_ExamId",
                table: "ExamSelectionRules",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_Courses_CourseId",
                table: "ExamQuestions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_Courses_CourseId",
                table: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "ExamAttemptQuestions");

            migrationBuilder.DropTable(
                name: "ExamSelectionRules");

            migrationBuilder.DropIndex(
                name: "IX_ExamQuestions_CourseId",
                table: "ExamQuestions");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "ExamQuestions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ExamQuestions");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "ExamQuestions",
                newName: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamId",
                table: "ExamQuestions",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_Exams_ExamId",
                table: "ExamQuestions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
