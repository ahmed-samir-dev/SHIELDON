using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;
using System;

namespace SHIELDON.Tests.Fixtures;

public class DbContextFixture : IDisposable
{
    public AppDbContext DbContext { get; private set; }

    public User AdminUser { get; private set; } = null!;
    public User TutorUser { get; private set; } = null!;
    public User StudentUser { get; private set; } = null!;
    public Course SampleCourse { get; private set; } = null!;

    public DbContextFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShieldonTestDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new AppDbContext(options);
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        AdminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@shieldon.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            FirstName = "System",
            LastName = "Admin",
            Role = UserRole.Admin,
            AccountStatus = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        TutorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "tutor@shieldon.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tutor@123456"),
            FirstName = "Jane",
            LastName = "Tutor",
            Role = UserRole.Tutor,
            AccountStatus = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        StudentUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@shieldon.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123456"),
            FirstName = "John",
            LastName = "Student",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.AddRange(AdminUser, TutorUser, StudentUser);

        SampleCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Computer Security 101",
            CourseCode = "SEC101",
            Description = "Fundamentals of Cybersecurity & Anti-Cheating Architecture",
            AssignedTutorId = TutorUser.Id,
            AssignedTutor = TutorUser,
            CreatedByAdminId = AdminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Courses.Add(SampleCourse);
        DbContext.SaveChanges();
    }

    public static AppDbContext CreateUniqueDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShieldonTestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}
