using System.Diagnostics;
using System.Reflection;
using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;
using Infrastructure.DAL.Repository.Implementations;
using Infrastructure.Initializers;
using Microsoft.OpenApi.Models;
using Shared.Encrypt;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using TelegramAPI.Services;

namespace TelegramAPI;

public static class Program
{
    // контролируем режим из конфигурации
    public static bool UseWebHook;
    public static string? WebhookUrl;

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;

        ConfigureServices(builder);

        var app = builder.Build();

        await ConfigurePipelineAsync(app);

       await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var conn = config.GetValue<string>("DB_CONNECTION") ??
                   "Host=localhost;Port=5435;Database=deputy;Username=postgres;Password=postgres";

        builder.Services.InitializeDatabase(conn);

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

        // Controllers + Swagger + HttpClient
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpClient();

        ConfigureSwagger(builder);

        // Telegram - регистрация и выбор режима
        ConfigureTelegramNotify(builder);
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type =>
            {
                return type.FullName!.Replace("+", ".");
            });

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
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });
    }

    private static void ConfigureTelegramNotify(WebApplicationBuilder builder)
    {
        // читаем конфиг один раз при старте
        UseWebHook = builder.Configuration.GetValue<bool>("TELEGRAM_USE_WEBHOOK");
        WebhookUrl = builder.Configuration["TELEGRAM_WEBHOOK_URL"];

        var tgToken = builder.Configuration.GetValue<string>("TELEGRAM_BOT_TOKEN") ??
                      builder.Configuration.GetValue<string>("Telegram:Token") ?? "";

        // singleton client
        builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(tgToken));

        // core handlers
        builder.Services.AddScoped<TgEventNotificationHandler>();
        builder.Services.AddScoped<IChatRepository, ChatRepository>();

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

    private static async Task ConfigurePipelineAsync(WebApplication app)
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Telegram API v1");
            c.RoutePrefix = string.Empty;
        });
        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

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
                Trace.TraceInformation("SetWebhook result: {Result}", setResult);

                var info = await botClient.SendRequest(new GetWebhookInfoRequest(), ct);
                Trace.TraceInformation("Webhook url: {Url}, pending: {Pending}", info?.Url, info?.PendingUpdateCount);
            }
            else
            {
                // удаляем webhook чтобы polling не конфликтовал
                var delResult = await botClient.SendRequest(new DeleteWebhookRequest { DropPendingUpdates = true }, ct);
                Trace.TraceInformation("DeleteWebhook result: {Result}", delResult);

                var info = await botClient.SendRequest(new GetWebhookInfoRequest(), ct);
                Trace.TraceInformation("Webhook info after delete: {Url}", info?.Url ?? "<none>");
            }
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Telegram webhook/polling init failed: {ex}");
        }
    }
}