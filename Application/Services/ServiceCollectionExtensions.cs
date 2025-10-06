using Application.Services.Abstractions;
using Application.Services.Implementations;
using DeputyApp.DAL.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Shared.Encrypt;

namespace Application.Services;

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
        return services;
    }
}