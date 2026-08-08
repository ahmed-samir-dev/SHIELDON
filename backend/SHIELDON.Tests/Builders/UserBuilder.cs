using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using System;

namespace SHIELDON.Tests.Builders;

public class UserBuilder
{
    private User _user;

    public UserBuilder()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test_{Guid.NewGuid():N}@shieldon.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword@123"),
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Student,
            AccountStatus = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public UserBuilder WithRole(UserRole role)
    {
        _user.Role = role;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public User Build() => _user;
}
