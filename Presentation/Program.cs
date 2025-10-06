using System.Reflection;
using System.Security.Claims;
using System.Text;
using Application.Notifications;
using Application.Services;
using Application.Services.Abstractions;
using Application.Storage;
using DeputyApp.DAL.UnitOfWork;
using Infrastructure.DAL;
using Infrastructure.Initializers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Encrypt;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var conn = config.GetValue<string>("DB_CONNECTION") ??
           "Host=localhost;Port=5435;Database=deputy;Username=postgres;Password=postgres";

builder.Services.InitializeDatabase(conn);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
            RoleClaimType = ClaimTypes.Role
        };
    });

var minioEndpoint = config.GetValue<string>("MINIO_ENDPOINT") ?? "minio:9000";
var minioAccess = config.GetValue<string>("MINIO_ACCESS_KEY") ?? "minioadmin";
var minioSecret = config.GetValue<string>("MINIO_SECRET_KEY") ?? "minioadmin";
var minioBucket = config.GetValue<string>("MINIO_BUCKET") ?? "deputy-files";
builder.Services.AddSingleton<IFileStorage>(_ =>
    new MinioFileStorage(minioEndpoint, minioAccess, minioSecret, minioBucket));

var tgToken = config.GetValue<string>("TELEGRAM_BOT_TOKEN") ?? "";
var tgChat = config.GetValue<string>("TELEGRAM_CHAT_ID") ?? "";
builder.Services.AddHttpClient<INotificationService, TelegramNotificationService>(client =>
    {
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
builder.Services.AddSingleton<INotificationService>(sp =>
    new TelegramNotificationService(sp.GetRequiredService<HttpClient>(), tgToken, tgChat));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "������� 'Bearer' [������] � ��� �����",
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

app.UseCors("AllowAll");

using var scope = app.Services.CreateScope();
await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
await DbContextInitializer.Migrate(appDbContext, hasher);


app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<JwtBlacklistMiddleware>();

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();