using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "QuestionOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Exams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "ExamQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "CustomEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "CourseMaterials",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Announcements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Translations",
                table: "QuestionOptions");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "ExamQuestions");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "CustomEvents");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "CourseMaterials");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Announcements");
        }
    }
}
