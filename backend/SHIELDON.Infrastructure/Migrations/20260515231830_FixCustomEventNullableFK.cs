using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomEventNullableFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomEvents_Courses_CourseId",
                table: "CustomEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomEvents_Courses_CourseId",
                table: "CustomEvents",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomEvents_Courses_CourseId",
                table: "CustomEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomEvents_Courses_CourseId",
                table: "CustomEvents",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
