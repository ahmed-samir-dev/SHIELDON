using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ChatServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ChatServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetInboxAsync_WithEmptyInbox_ShouldReturnEmptyList()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ChatService(dbContext);

        // Act
        var result = await service.GetInboxAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }
}
