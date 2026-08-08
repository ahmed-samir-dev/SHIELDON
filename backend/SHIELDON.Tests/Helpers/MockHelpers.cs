using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SHIELDON.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Helpers;

public static class MockHelpers
{
    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static IConfiguration CreateConfigurationMock(Dictionary<string, string?>? settings = null)
    {
        settings ??= new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "SUPER_SECRET_TESTING_JWT_KEY_MIN_32_BYTES_LONG_12345"},
            {"JwtSettings:Issuer", "SHIELDON-Test"},
            {"JwtSettings:Audience", "SHIELDON-Test-Audience"},
            {"JwtSettings:AccessTokenExpiryMinutes", "60"},
            {"JwtSettings:RefreshTokenExpiryDays", "7"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    public static Mock<IEmailService> CreateEmailServiceMock()
    {
        var mock = new Mock<IEmailService>();
        return mock;
    }
}
