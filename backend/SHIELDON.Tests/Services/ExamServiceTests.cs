using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ExamServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ExamServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetExamsAsync_WithValidCourse_ShouldReturnExams()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "CS401", Title = "DB", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        dbContext.Exams.Add(new Exam { Id = Guid.NewGuid(), CourseId = courseId, CreatedByUserId = adminId, Title = "Midterm", TimeLimit = 60, PassScore = 60, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new ExamService(dbContext, Mock.Of<INotificationService>());

        // Act
        var result = await service.GetExamsAsync(courseId, new ExamQueryParams(), adminId, "Admin");

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetExamByIdAsync_WithInvalidId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ExamService(dbContext, Mock.Of<INotificationService>());

        // Act
        Func<Task> act = async () => await service.GetExamByIdAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
