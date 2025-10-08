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
using Shared.Encrypt;
using Shared.Middleware;
using Telegram.Bot;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var conn = config.GetValue<string>("DB_CONNECTION") ??
           "Host=localhost;Port=5435;Database=deputy;Username=postgres;Password=postgres";
builder.Services.InitializeDatabase(conn);

// Регистрация UnitOfWork и репозиториев
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

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

var minioOptions = new MinioOptions
{
    Endpoint = config.GetValue<string>("MINIO_ENDPOINT") ?? "localhost:9000",
    AccessKey = config.GetValue<string>("MINIO_ACCESS_KEY") ?? "minioadmin",
    SecretKey = config.GetValue<string>("MINIO_SECRET_KEY") ?? "minioadmin",
    Bucket = config.GetValue<string>("MINIO_BUCKET") ?? "deputy-files"
};
builder.Services.AddSingleton(minioOptions);
builder.Services.AddSingleton<IFileStorage, MinioFileStorage>();

var tgToken = config.GetValue<string>("TELEGRAM_BOT_TOKEN") ?? "";
builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(tgToken));
builder.Services.AddSingleton(sp => new TelegramNotificationService(
    sp.GetRequiredService<ITelegramBotClient>(), 
    defaultChatId: null
));
builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddScoped<TelegramMessageHandler>();
builder.Services.AddScoped<EventNotificationHandler>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

var app = builder.Build();

using var scope = app.Services.CreateScope();
await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
await DbContextInitializer.Migrate(appDbContext, hasher);

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

await app.RunAsync();
