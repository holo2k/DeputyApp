using Domain.Entities;
using Infrastructure.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Encrypt;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Initializers;

public static class DbContextInitializer
{
    public static void InitializeDatabase(this IServiceCollection services, string conn)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(conn));
    }

    /// <summary>
    ///     Попытаться применить миграции с повторными попытками.
    /// </summary>
    public static async Task Migrate(AppDbContext context, IPasswordHasher passwordHasher, int maxAttempts = 12)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var attempt = 0;
        var delay = TimeSpan.FromSeconds(2);

        while (true)
            try
            {
                attempt++;
                await context.Database.MigrateAsync();
                Console.WriteLine("Database migrated successfully.");
                if (context.Users.Any(x => x.UserRoles.Any(r => r.Role.Name == "Admin")))
                    return;

                var userId = Guid.CreateVersion7();
                var roleId = Guid.CreateVersion7();
                var salt = Guid.NewGuid().ToString();
                var password = "admin";

                var user = new User
                {
                    Id = userId,
                    CreatedAt = DateTime.UtcNow,
                    Email = "admin@admin.ru",
                    JobTitle = "admin",
                    Salt = salt,
                    FullName = "Admin"
                };

                user.PasswordHash = passwordHasher.HashPassword(password, salt);

                context.Users.Add(user);
                context.Roles.Add(new Role { Id = roleId, Name = "Admin" });

                context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                });

                await context.SaveChangesAsync();
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                if (attempt >= maxAttempts)
                {
                    Console.WriteLine($"Database migration failed after {attempt} attempts: {ex.Message}");
                    throw;
                }

                Console.WriteLine(
                    $"Database not ready (attempt {attempt}/{maxAttempts}): {ex.Message}. Waiting {delay.TotalSeconds}s before retry.");
                await Task.Delay(delay);
                // экспоненциальный бэк-офф, но с ограничением
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
    }

    private static bool IsTransient(Exception ex)
    {
        // Подхватываем Npgsql connection errors и сокет-ошибки или любые ошибки подключения к БД.
        var t = ex;
        while (t != null)
        {
            var name = t.GetType().FullName ?? string.Empty;
            if (name.Contains("Npgsql") || name.Contains("SocketException") || name.Contains("TimeoutException"))
                return true;
            t = t.InnerException;
        }

        return false;
    }
}