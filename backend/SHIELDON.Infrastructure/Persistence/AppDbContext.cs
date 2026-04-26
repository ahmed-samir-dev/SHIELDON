using SHIELDON.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace SHIELDON.Infrastructure.Persistence;

/// <summary>
/// The main Entity Framework Core database context for SHIELDON.
/// All entities are registered here. All database access flows through this class.
///
/// Convention: PascalCase table and column names (configured per entity).
/// All timestamps stored in UTC (DATETIME2 in SQL Server).
/// All primary keys are GUID (UNIQUEIDENTIFIER).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Phase 1: Auth ──────────────────────────────────────────────
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LoginActivityLog> LoginActivityLogs { get; set; } = null!;
    public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;

    // ── Phase 2: Core LMS ──────────────────────────────────────────
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<CourseEnrollment> CourseEnrollments { get; set; } = null!;
    public DbSet<CourseMaterial> CourseMaterials { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;

    // ── Phase 3: Examination & Grading ──────────────────────────────
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<ExamQuestion> ExamQuestions { get; set; } = null!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
    public DbSet<ExamAttempt> ExamAttempts { get; set; } = null!;
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; } = null!;
    public DbSet<ExamToken> ExamTokens { get; set; } = null!;
    public DbSet<GradeRecord> GradeRecords { get; set; } = null!;
    public DbSet<ReattemptRequest> ReattemptRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes found in this assembly
        // Each entity has its own configuration file in Persistence/Configurations/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
