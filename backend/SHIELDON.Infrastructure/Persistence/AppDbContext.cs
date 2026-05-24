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

    // Question Bank - course-level, shared across exams
    public DbSet<ExamQuestion> ExamQuestions { get; set; } = null!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;

    // Exam configuration
    public DbSet<ExamSelectionRule> ExamSelectionRules { get; set; } = null!;

    // Attempt lifecycle
    public DbSet<ExamAttempt> ExamAttempts { get; set; } = null!;
    public DbSet<ExamAttemptQuestion> ExamAttemptQuestions { get; set; } = null!;
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; } = null!;
    public DbSet<ExamToken> ExamTokens { get; set; } = null!;
    public DbSet<GradeRecord> GradeRecords { get; set; } = null!;
    public DbSet<ReattemptRequest> ReattemptRequests { get; set; } = null!;

    // ── v1.1: Exam Re-open Extensions ──────────────────────────────
    /// <summary>Grants specific students access to an exam after its global EndTime has passed.</summary>
    public DbSet<ExamExtension> ExamExtensions { get; set; } = null!;

    // ── Phase 4: Anti-Cheating Engine (used by monitoring dashboards) ──────────
    public DbSet<ViolationLog> ViolationLogs { get; set; } = null!;

    // ── Phase 6: Real-time Chat ────────────────────────────────────────────────
    public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    // ── Phase 6: Attendance Tracking ──────────────────────────────────────────
    public DbSet<AttendanceCheck> AttendanceChecks { get; set; } = null!;
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;

    // ── Phase 6: Calendar & Schedule ──────────────────────────────────────────
    public DbSet<CustomEvent> CustomEvents { get; set; } = null!;

    // ── Phase 6.5: Payment Gateway ────────────────────────────────────────────
    public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes found in this assembly
        // Each entity has its own configuration file in Persistence/Configurations/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
