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

public class CourseServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public CourseServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateCourseAsync_WithValidData_ShouldCreateCourse()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var adminId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext, Mock.Of<INotificationService>());
        var request = new CreateCourseRequest("Intro to CS", "CS101", "Description", null, 0);

        // Act
        var result = await service.CreateCourseAsync(adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.CourseCode.Should().Be("CS101");
        var created = await dbContext.Courses.SingleOrDefaultAsync(c => c.CourseCode == "CS101");
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCourseAsync_WithDuplicateCode_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var adminId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin2@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Courses.Add(new Course { Id = Guid.NewGuid(), CourseCode = "CS102", Title = "Existing", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext, Mock.Of<INotificationService>());
        var request = new CreateCourseRequest("Duplicate CS", "CS102", "Description", null, 0);

        // Act
        Func<Task> act = async () => await service.CreateCourseAsync(adminId, request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already in use*");
    }

    [Fact]
    public async Task GetCourseByIdAsync_WithValidId_ShouldReturnCourse()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "CS103", Title = "CS 103", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new CourseService(dbContext, Mock.Of<INotificationService>());

        // Act
        var result = await service.GetCourseByIdAsync(courseId);

        // Assert
        result.Should().NotBeNull();
        result.CourseCode.Should().Be("CS103");
    }
}
