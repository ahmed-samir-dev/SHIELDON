using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefinePhoneOtpTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneOtpCode",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneVerificationStatus",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'None'",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneOtpLastSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneOtpLastSentAt",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneVerificationStatus",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'None'");

            migrationBuilder.AddColumn<string>(
                name: "PhoneOtpCode",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
