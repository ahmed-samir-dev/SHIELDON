using SHIELDON.Application.Interfaces;
using SHIELDON.Application.Features.Calendar.Interfaces;
using SHIELDON.Application.Features.Payment.Interfaces;
using SHIELDON.Infrastructure.BackgroundServices;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Infrastructure.Data.Interceptors;
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
        // ── Singletons ───────────────────────────────────────────────────
        services.AddSingleton<AutoTranslationInterceptor>();
        services.AddSingleton<TranslationMaterializationInterceptor>();
        services.AddHttpContextAccessor();

        // ── Database ──────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
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
                });

            // Register our Translation Interceptors
            var autoInterceptor = sp.GetRequiredService<AutoTranslationInterceptor>();
            var matInterceptor = sp.GetRequiredService<TranslationMaterializationInterceptor>();
            options.AddInterceptors(autoInterceptor, matInterceptor);
        });

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
        services.AddScoped<IExamAttemptService, ExamAttemptService>();
        services.AddScoped<IExamResultService, ExamResultService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IViolationService, ViolationService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICurrentLanguageProvider, CurrentLanguageProvider>();
        services.AddSingleton<PresenceTracker>();

        // ── HTTP Clients ───────────────────────────────────────────────────
        // Named client used by AIService to call the Gemini REST API
        services.AddHttpClient("Gemini");

        // Register Free Translation API
        services.AddHttpClient<ITranslationService, LingvaTranslationService>();

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
