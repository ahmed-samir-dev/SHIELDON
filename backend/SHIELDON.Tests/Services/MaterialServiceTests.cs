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

public class MaterialServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public MaterialServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetMaterialsAsync_WithValidCourse_ShouldReturnMaterials()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Users.Add(new User { Id = adminId, Email = "admin@example.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "hash", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "CS301", Title = "Algorithms", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.CourseMaterials.Add(new CourseMaterial { Id = Guid.NewGuid(), CourseId = courseId, UploadedByUserId = adminId, Title = "Lecture 1", MaterialType = SHIELDON.Domain.Enums.MaterialType.Link, ExternalUrl = "https://example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new MaterialService(dbContext, Mock.Of<IWebHostEnvironment>(), Mock.Of<INotificationService>());

        // Act
        var list = await service.GetMaterialsAsync(courseId, adminId, "Admin");

        // Assert
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMaterialsAsync_WithInvalidCourse_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new MaterialService(dbContext, Mock.Of<IWebHostEnvironment>(), Mock.Of<INotificationService>());

        // Act
        Func<Task> act = async () => await service.GetMaterialsAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
