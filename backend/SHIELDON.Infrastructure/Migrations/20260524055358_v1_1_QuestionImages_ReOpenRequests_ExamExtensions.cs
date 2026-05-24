using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1_1_QuestionImages_ReOpenRequests_ExamExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "ReattemptRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReopenRequest",
                table: "ReattemptRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ExamQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamExtensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtendedEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamExtensions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamExtensions_ReattemptRequests_SourceRequestId",
                        column: x => x.SourceRequestId,
                        principalTable: "ReattemptRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamExtensions_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamExtensions_ExamId",
                table: "ExamExtensions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExtensions_SourceRequestId",
                table: "ExamExtensions",
                column: "SourceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExtensions_StudentId_ExamId",
                table: "ExamExtensions",
                columns: new[] { "StudentId", "ExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamExtensions");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "ReattemptRequests");

            migrationBuilder.DropColumn(
                name: "IsReopenRequest",
                table: "ReattemptRequests");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ExamQuestions");
        }
    }
}
