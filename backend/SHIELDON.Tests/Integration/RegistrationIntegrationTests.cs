using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SHIELDON.Application.Features.Auth.DTOs;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Application.Interfaces;
using Moq;

namespace SHIELDON.Tests.Integration;

/// <summary>
/// Integration tests for the POST /api/auth/register endpoint.
/// Uses WebApplicationFactory with an InMemory database to spin up the real HTTP pipeline.
/// </summary>
public class RegistrationIntegrationTests : IClassFixture<RegistrationWebAppFactory>
{
    private readonly RegistrationWebAppFactory _factory;

    public RegistrationIntegrationTests(RegistrationWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest(
            "Integration",
            "Test",
            $"integration_{Guid.NewGuid():N}@test.com", // Unique email per run
            "StrongPass123!",
            "StrongPass123!",
            UserRole.Student
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        var responseText = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: $"Response body: {responseText}");
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest(
            "Integration",
            "Test",
            "weak@test.com",
            "weak",       // Password too short - fails FluentValidation
            "weak",
            UserRole.Student
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>
/// A custom WebApplicationFactory that replaces SQL Server with an InMemory DB
/// and mocks external IEmailService so the host can execute without a real SMTP connection.
/// </summary>
public class RegistrationWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "InMemoryDb_Registration_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]                = "test-secret-key-for-integration-tests-must-be-long-enough-32+",
                ["JwtSettings:Issuer"]                   = "SHIELDON-Test",
                ["JwtSettings:Audience"]                 = "SHIELDON-Test-Users",
                ["JwtSettings:AccessTokenExpiryMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpiryDays"]   = "7",
                ["EmailSettings:Host"]                   = "localhost",
                ["EmailSettings:Port"]                   = "25",
                ["EmailSettings:Username"]               = "test",
                ["EmailSettings:Password"]               = "test",
                ["EmailSettings:FromAddress"]            = "noreply@test.com",
                ["EmailSettings:FromName"]               = "SHIELDON Test",
                ["AdminSeed:Email"]                      = "admin@test.com",
                ["AdminSeed:Password"]                   = "Admin@Test123!",
                ["AdminSeed:FirstName"]                  = "Test",
                ["AdminSeed:LastName"]                   = "Admin",
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1. Remove all existing DbContext options & context registrations to prevent dual-provider error
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType.Name.Contains("DbContext") ||
                d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 2. Add InMemory DB instead
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // 3. Mock IEmailService
            var emailServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
                services.Remove(emailServiceDescriptor);

            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);
            mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);

            services.AddSingleton(mockEmailService.Object);
        });
    }
}


