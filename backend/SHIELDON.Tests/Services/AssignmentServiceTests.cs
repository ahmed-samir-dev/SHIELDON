using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class AssignmentServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public AssignmentServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAssignmentsAsync_WithValidCourse_ShouldReturnAssignments()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "CS201", Title = "Data Structures", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Assignments.Add(new Assignment { Id = Guid.NewGuid(), CourseId = courseId, CreatedByUserId = adminId, Title = "Hw1", Instructions = "Desc", Weight = 10, DueDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new AssignmentService(dbContext, Mock.Of<IWebHostEnvironment>(), Mock.Of<INotificationService>());

        // Act
        var list = await service.GetAssignmentsAsync(courseId, adminId, "Admin");

        // Assert
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAssignmentsAsync_WithInvalidCourse_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new AssignmentService(dbContext, Mock.Of<IWebHostEnvironment>(), Mock.Of<INotificationService>());

        // Act
        Func<Task> act = async () => await service.GetAssignmentsAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
