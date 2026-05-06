using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "AssignmentSubmissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsAwarded",
                table: "AssignmentSubmissions",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "AssignmentSubmissions",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "AssignmentSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_ReviewedById",
                table: "AssignmentSubmissions",
                column: "ReviewedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_Users_ReviewedById",
                table: "AssignmentSubmissions",
                column: "ReviewedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_Users_ReviewedById",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_ReviewedById",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Assignments");
        }
    }
}
