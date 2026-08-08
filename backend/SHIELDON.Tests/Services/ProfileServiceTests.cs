using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ProfileServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ProfileServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetProfileAsync_WithValidUserId_ShouldReturnProfile()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            Email = "profile@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Student,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new ProfileService(dbContext, Mock.Of<IFileService>(), Mock.Of<IOtpService>());

        // Act
        var result = await service.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("profile@example.com");
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ProfileService(dbContext, Mock.Of<IFileService>(), Mock.Of<IOtpService>());

        // Act
        Func<Task> act = async () => await service.GetProfileAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
