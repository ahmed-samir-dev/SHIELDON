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
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public AuthServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockJwtService = new Mock<IJwtService>();

        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldCreateUserAndSendEmail()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new RegisterRequest("Test", "User", "test@example.com", "Password123!", "Password123!", UserRole.Student);

        // Act
        await authService.RegisterAsync(request);

        // Assert
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == "test@example.com");
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Test");
        user.AccountStatus.Should().Be(AccountStatus.Unverified);
        user.VerificationCode.Should().NotBeNullOrEmpty();
        
        _mockEmailService.Verify(e => e.SendEmailVerificationAsync(user.Email, user.FullName, user.VerificationCode!), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        dbContext.Users.Add(new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "exist@example.com", 
            PasswordHash = "hash", 
            FirstName = "E", 
            LastName = "E", 
            Role = UserRole.Student, 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        });
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new RegisterRequest("Test", "User", "exist@example.com", "Password123!", "Password123!", UserRole.Student);

        // Act
        Func<Task> act = async () => await authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("An account with this email already exists.");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var rawPassword = "ValidPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "validuser@example.com",
            PasswordHash = passwordHash,
            FirstName = "Valid",
            LastName = "User",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock_access_token");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("mock_refresh_token");

        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new LoginRequest("validuser@example.com", rawPassword);

        // Act
        var response = await authService.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("mock_access_token");
        response.RefreshToken.Should().Be("mock_refresh_token");
        response.Email.Should().Be("validuser@example.com");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldIncreaseFailedAttempts()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrongpass@example.com",
            PasswordHash = passwordHash,
            FirstName = "Wrong",
            LastName = "Pass",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Active,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new LoginRequest("wrongpass@example.com", "WrongPassword!");

        // Act
        Func<Task> act = async () => await authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        var updatedUser = await dbContext.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidCode_ShouldActivateUser()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "unverified@example.com",
            PasswordHash = "hash",
            FirstName = "Unverified",
            LastName = "User",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Unverified,
            VerificationCode = "123456",
            VerificationCodeExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new VerifyEmailRequest("unverified@example.com", "123456");

        // Act
        await authService.VerifyEmailAsync(request);

        // Assert
        var updatedUser = await dbContext.Users.FindAsync(user.Id);
        updatedUser!.AccountStatus.Should().Be(AccountStatus.Active);
        updatedUser.EmailVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidCode_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "invalidcode@example.com",
            PasswordHash = "hash",
            FirstName = "Inv",
            LastName = "Code",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Unverified,
            VerificationCode = "123456",
            VerificationCodeExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new VerifyEmailRequest("invalidcode@example.com", "999999");

        // Act
        Func<Task> act = async () => await authService.VerifyEmailAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var authService = new AuthService(dbContext, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);
        var request = new LoginRequest("nobody@example.com", "Password123!");

        // Act
        Func<Task> act = async () => await authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}


