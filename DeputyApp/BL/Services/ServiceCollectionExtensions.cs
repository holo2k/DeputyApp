using DeputyApp.BL.Encrypt;
using DeputyApp.BL.Services.Abstractions;
using DeputyApp.BL.Services.Implementations;
using DeputyApp.DAL.UnitOfWork;

namespace DeputyApp.BL.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeputyAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        // IFileStorage and INotificationService must be provided by the app (S3, MinIO, Telegram etc.)
        return services;
    }
}