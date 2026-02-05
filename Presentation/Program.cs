using System.Reflection;
using System.Security.Claims;
using System.Text;
using Application;
using Application.Notifications;
using Application.Services;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Application.Storage;
using Application.Validators;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;
using Infrastructure.DAL.Repository.Implementations;
using Infrastructure.Initializers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Hub;
using Presentation.Middleware;
using Serilog;
using Serilog.Events;
using Shared.Encrypt;
using Shared.Middleware;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;


namespace Presentation;

public static class Program
{
    // контролируем режим из конфигурации
    public static bool UseWebHook;
    public static string? WebhookUrl;

    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;
        ConfigureLogging(builder);
        ConfigureServices(builder);
        var app = builder.Build();

        await InitializeDatabaseAsync(app);

        ConfigurePipeline(app); // async init

        try
        {
            Log.Information("Starting web host");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var conn = config.GetValue<string>("DB_CONNECTION") ??
                   "Host=localhost;Port=5435;Database=deputy;Username=postgres;Password=postgres";

        builder.Services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => 
                    { 
                        options.UseNpgsqlConnection(conn); 
                    }));

        builder.Services.AddSignalR();

        builder.Services.AddHangfireServer();

        builder.Services.InitializeDatabase(conn);

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
        builder.Services.AddScoped<IPhoneNotificationService, PhoneNotificationService>();
        builder.Services.AddScoped<IScheduleService<Event>, ScheduleService<Event>>();

        builder.Services.AddSingleton<IBlackListService, BlackListService>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddDeputyAppServices();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        ConfigureJwtAuthentication(builder);

        var minioOptions = new MinioOptions
        {
            Endpoint = config.GetValue<string>("MINIO_ENDPOINT") ?? "localhost:9000",
            AccessKey = config.GetValue<string>("MINIO_ACCESS_KEY") ?? "minioadmin",
            SecretKey = config.GetValue<string>("MINIO_SECRET_KEY") ?? "minioadmin",
            Bucket = config.GetValue<string>("MINIO_BUCKET") ?? "deputy-files"
        };
        builder.Services.AddSingleton(minioOptions);
        builder.Services.AddSingleton<IFileStorage, MinioFileStorage>();

        // Controllers + Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpClient();

        ConfigureSwagger(builder);

        // Telegram - регистрация и выбор режима
        ConfigureTelegramNotify(builder);
    }

    private static void ConfigureJwtAuthentication(WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
                if (string.IsNullOrEmpty(jwtKey))
                    throw new ArgumentNullException("JWT_KEY не задан");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    RoleClaimType = ClaimTypes.Role
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Deputy API",
                Version = "v1",
                Description = "API для депутатского приложения"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Введите 'Bearer' [пробел] и ваш JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        });
    }

    private static void ConfigureTelegramNotify(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TelegramMessageHandler>();
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var appDbContext = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();
        await DbContextInitializer.Migrate(appDbContext, hasher);
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseCors("AllowAll");

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Deputy API v1");
            c.RoutePrefix = string.Empty;
        });

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseMiddleware<JwtBlacklistMiddleware>();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire");

        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapControllers();
    }
}