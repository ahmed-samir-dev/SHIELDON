using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneNumber')
                    ALTER TABLE [Users] ADD [PhoneNumber] nvarchar(20) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneOtpCode')
                    ALTER TABLE [Users] ADD [PhoneOtpCode] nvarchar(10) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneOtpExpiresAt')
                    ALTER TABLE [Users] ADD [PhoneOtpExpiresAt] datetime2 NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneOtpFailedAttempts')
                    ALTER TABLE [Users] ADD [PhoneOtpFailedAttempts] int NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneVerificationStatus')
                    ALTER TABLE [Users] ADD [PhoneVerificationStatus] nvarchar(20) NOT NULL DEFAULT 'None';

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'PhoneVerifiedAt')
                    ALTER TABLE [Users] ADD [PhoneVerifiedAt] datetime2 NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneOtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneOtpExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneOtpFailedAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                table: "Users");
        }
    }
}
