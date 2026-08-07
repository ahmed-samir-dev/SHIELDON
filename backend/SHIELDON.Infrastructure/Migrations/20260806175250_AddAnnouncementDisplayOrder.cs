using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Announcements",
                type: "INT",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Announcements");
        }
    }
}
