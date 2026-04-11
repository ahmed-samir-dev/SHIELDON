using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Auth.DTOs;

namespace SHIELDON.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IJwtService> _mockJwtProvider;
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public AuthServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockJwtProvider = new Mock<IJwtService>();

        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldCreateUserAndSendEmail()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var authService = new AuthService(dbContext, _mockJwtProvider.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new RegisterRequest("Test", "User", "test@example.com", "Password123!", "Password123!", UserRole.Student);

        // Act
        await authService.RegisterAsync(request);

        // Assert
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == "test@example.com");
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Test");
        user.AccountStatus.Should().Be(AccountStatus.Unverified);
        user.VerificationCode.Should().NotBeNullOrEmpty();
        
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync(user.Email, user.FullName, user.VerificationCode), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        dbContext.Users.Add(new User { Id = Guid.NewGuid(), Email = "exist@example.com", PasswordHash = "hash", FirstName = "E", LastName = "E", Role = UserRole.Student, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, _mockJwtProvider.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new RegisterRequest("Test", "User", "exist@example.com", "Password123!", "Password123!", UserRole.Student);

        // Act
        Func<Task> act = async () => await authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("An account with this email already exists.");
    }
}
