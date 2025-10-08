using System.Reflection;
using System.Security.Claims;
using System.Text;
using Application.Notifications;
using Application.Services;
using Application.Services.Abstractions;
using Application.Storage;
using DeputyApp.DAL.UnitOfWork;
using DotNetEnv;
using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;
using Infrastructure.DAL.Repository.Implementations;
using Infrastructure.Initializers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Shared.Encrypt;
using Shared.Middleware;
using Telegram.Bot;

namespace Presentation;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;

        ConfigureLogging(builder);
        ConfigureServices(builder);

        var app = builder.Build();

        await InitializeDatabaseAsync(app);

        ConfigurePipeline(app);

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

        // DbContext registration
        builder.Services.InitializeDatabase(conn);

        // Repositories / UoW
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Infrastructure and helpers
        builder.Services.AddSingleton<IBlackListService, BlackListService>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Application services
        builder.Services.AddDeputyAppServices();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Authentication JWT
        ConfigureJwtAuthentication(builder);

        // MinIO storage options + registration
        var minioOptions = new MinioOptions
        {
            Endpoint = config.GetValue<string>("MINIO_ENDPOINT") ?? "localhost:9000",
            AccessKey = config.GetValue<string>("MINIO_ACCESS_KEY") ?? "minioadmin",
            SecretKey = config.GetValue<string>("MINIO_SECRET_KEY") ?? "minioadmin",
            Bucket = config.GetValue<string>("MINIO_BUCKET") ?? "deputy-files"
        };
        builder.Services.AddSingleton(minioOptions);
        builder.Services.AddSingleton<IFileStorage, MinioFileStorage>();

        // Telegram notifier
        var tgToken = config.GetValue<string>("TELEGRAM_BOT_TOKEN") ?? "";
        var tgChat = config.GetValue<string>("TELEGRAM_CHAT_ID") ?? "";
        builder.Services.AddHttpClient<INotificationService, TelegramNotificationService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
        builder.Services.AddSingleton<INotificationService>(sp =>
            new TelegramNotificationService(sp.GetRequiredService<HttpClient>(), tgToken, tgChat));

        // Controllers + Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        ConfigureSwagger(builder);
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
                    throw new Exception("JWT_KEY не задан");

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
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        });
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var appDbContext = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();

        // Migrate with retries. DbContextInitializer handles retry/.
        await DbContextInitializer.Migrate(appDbContext, hasher);
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Deputy API v1");
            c.RoutePrefix = string.Empty;
        });

        app.UseCors("AllowAll");

        app.UseMiddleware<JwtBlacklistMiddleware>();

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }
}