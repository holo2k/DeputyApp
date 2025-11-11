using Application.Notifications;
using Application.Services;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Application.Storage;
using DeputyApp.DAL.UnitOfWork;
using DotNetEnv;
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
using Serilog;
using Serilog.Events;
using Shared.Encrypt;
using Shared.Middleware;
using System.Reflection;
using System.Security.Claims;
using System.Text;
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

        await ConfigurePipelineAsync(app); // async init webhook/polling

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

        builder.Services.AddSingleton<IBlackListService, BlackListService>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddDeputyAppServices();
        builder.Services.AddScoped<IEventService, EventService>();

        var tgChatId = config.GetValue<string>("TELEGRAM_CHAT_ID") ?? "";

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
        // читаем конфиг один раз при старте
        UseWebHook = builder.Configuration.GetValue<bool>("Telegram:UseWebhook");
        WebhookUrl = builder.Configuration["Telegram:WebhookUrl"];

        var tgToken = builder.Configuration.GetValue<string>("TELEGRAM_BOT_TOKEN") ??
                      builder.Configuration.GetValue<string>("Telegram:Token") ?? "";

        // singleton client
        builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(tgToken));

        // core handlers
        builder.Services.AddScoped<TelegramMessageHandler>();
        builder.Services.AddScoped<TgEventNotificationHandler>();

        // notification service (можно передать default chat id из env)
        var defaultChatId = builder.Configuration.GetValue<string>("TELEGRAM_CHAT_ID");

        // регистрируем конкретную реализацию как singleton
        builder.Services.AddSingleton<TelegramNotificationService>(sp =>
            new TelegramNotificationService(sp.GetRequiredService<ITelegramBotClient>(), defaultChatId));

        // связываем интерфейс с той же инстанцией
        builder.Services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<TelegramNotificationService>());

        // hosted polling worker только если не webhook
        if (!UseWebHook)
            builder.Services.AddHostedService<TelegramBotWorker>();
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var appDbContext = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();
        await DbContextInitializer.Migrate(appDbContext, hasher);
    }

    private static async Task ConfigurePipelineAsync(WebApplication app)
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
        app.UseHangfireDashboard("/hangfire");
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapControllers();

        // Telegram webhook / polling init
        var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
        var ct = CancellationToken.None;

        try
        {
            if (UseWebHook)
            {
                if (string.IsNullOrEmpty(WebhookUrl))
                    throw new Exception("WebhookUrl не задан в конфигурации");

                var setReq = new SetWebhookRequest
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                    Url = WebhookUrl
                };
                var setResult = await botClient.SendRequest(setReq, ct);
                Log.Information("SetWebhook result: {Result}", setResult);

                var info = await botClient.SendRequest(new GetWebhookInfoRequest(), ct);
                Log.Information("Webhook url: {Url}, pending: {Pending}", info?.Url, info?.PendingUpdateCount);
            }
            else
            {
                // удаляем webhook чтобы polling не конфликтовал
                var delResult = await botClient.SendRequest(new DeleteWebhookRequest { DropPendingUpdates = true }, ct);
                Log.Information("DeleteWebhook result: {Result}", delResult);

                var info = await botClient.SendRequest(new GetWebhookInfoRequest(), ct);
                Log.Information("Webhook info after delete: {Url}", info?.Url ?? "<none>");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Telegram webhook/polling init failed");
            // не падаем полностью, но логируем и продолжаем
        }
    }
}