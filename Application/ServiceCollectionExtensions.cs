using Application.Services.Abstractions;
using Application.Services.Implementations;
using Application.Storage;
using Application.Validators;
using DeputyApp.DAL.UnitOfWork;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Encrypt;

namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeputyAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IStatusService, StatusService>();
        services.AddValidatorsFromAssemblyContaining<ExpectedEndDateValidator>();
        services.AddValidatorsFromAssemblyContaining<TaskRequestValidator>();
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters(); 
        
        return services;
    }
}