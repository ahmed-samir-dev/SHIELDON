using SHIELDON.Application.Interfaces;
using SHIELDON.Infrastructure.BackgroundServices;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SHIELDON.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
/// Called once from SHIELDON.API/Program.cs.
/// This is the ONLY place the API layer is allowed to know about Infrastructure implementations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    // Automatically retry on transient failures (network blip, SQL restart)
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    // Fail fast if a query takes too long
                    sqlOptions.CommandTimeout(30);
                }));

        // ── Services ──────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IReattemptService, ReattemptService>();
        services.AddScoped<IQuestionService, QuestionService>();

        // ── Background Services ────────────────────────────────────────────
        services.AddHostedService<ExamReminderBackgroundService>();


        // ── AutoMapper 13.x ── AddAutoMapper scans assemblies for Profile classes
        services.AddAutoMapper(
            typeof(SHIELDON.Domain.Entities.User).Assembly,           // Domain
            typeof(SHIELDON.Application.Common.ApiResponse<>).Assembly, // Application
            typeof(DependencyInjection).Assembly);                     // Infrastructure

        // ── Memory Cache ──────────────────────────────────────────────────
        services.AddMemoryCache();

        return services;
    }
}
