using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class AnnouncementServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public AnnouncementServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateAnnouncementAsync_WithValidData_ShouldCreateAnnouncement()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var adminId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "C1", Title = "Course 1", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new AnnouncementService(dbContext, Mock.Of<INotificationService>());
        var request = new CreateAnnouncementRequest("Welcome", "Welcome to class", "Normal");

        // Act
        var result = await service.CreateAnnouncementAsync(courseId, request, adminId, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Welcome");
    }

    [Fact]
    public async Task GetAnnouncementsAsync_ShouldReturnAnnouncements()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin2@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "C1", Title = "Course 1", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Announcements.Add(new Announcement { Id = Guid.NewGuid(), CourseId = courseId, CreatedByUserId = adminId, Title = "Ann 1", Content = "Content", Priority = AnnouncementPriority.Normal, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new AnnouncementService(dbContext, Mock.Of<INotificationService>());

        // Act
        var list = await service.GetAnnouncementsAsync(courseId, adminId, "Admin");

        // Assert
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_ForNonExistentCourse_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new AnnouncementService(dbContext, Mock.Of<INotificationService>());
        var request = new CreateAnnouncementRequest("Welcome", "Welcome to class", "Normal");

        // Act
        Func<Task> act = async () => await service.CreateAnnouncementAsync(Guid.NewGuid(), request, Guid.NewGuid(), "Admin");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
