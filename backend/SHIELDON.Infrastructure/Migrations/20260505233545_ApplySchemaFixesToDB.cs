using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHIELDON.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplySchemaFixesToDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [ReattemptRequests] DROP CONSTRAINT [FK_ReattemptRequests_Exams_ExamId];
                ALTER TABLE [ReattemptRequests] ADD CONSTRAINT [FK_ReattemptRequests_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE;
                
                ALTER TABLE [Assignments] ALTER COLUMN [Weight] decimal(5,2) NOT NULL;
                ALTER TABLE [Exams] ALTER COLUMN [Weight] decimal(5,2) NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE [ReattemptRequests] DROP CONSTRAINT [FK_ReattemptRequests_Exams_ExamId];
                ALTER TABLE [ReattemptRequests] ADD CONSTRAINT [FK_ReattemptRequests_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION;
                
                ALTER TABLE [Assignments] ALTER COLUMN [Weight] decimal(18,2) NOT NULL;
                ALTER TABLE [Exams] ALTER COLUMN [Weight] decimal(18,2) NOT NULL;
            ");
        }
    }
}
