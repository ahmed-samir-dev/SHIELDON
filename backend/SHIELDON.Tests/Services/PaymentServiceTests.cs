using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Payment.DTOs;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class PaymentServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public PaymentServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WithNoPayments_ShouldReturnEmptyList()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new PaymentService(dbContext, Mock.Of<IConfiguration>(), Mock.Of<ILogger<PaymentService>>());

        // Act
        var result = await service.GetPaymentHistoryAsync(Guid.NewGuid(), "Student", new PaymentHistoryQueryParams());

        // Assert
        result.Items.Should().BeEmpty();
    }
}
