using Domain.Constants;
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
    /// Применяет миграции и выполняет сидинг с ретраями.
    /// </summary>
    public static async Task Migrate(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        int maxAttempts = 12)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (passwordHasher == null)
            throw new ArgumentNullException(nameof(passwordHasher));

        var attempt = 0;
        var delay = TimeSpan.FromSeconds(2);

        while (true)
        {
            try
            {
                attempt++;

                await context.Database.MigrateAsync();

                await EnsureAdminAsync(context, passwordHasher);
                await EnsureStatusesAsync(context);

                await context.SaveChangesAsync();

                Console.WriteLine("Database migrated and seeded successfully.");
                return;
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
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }
    }

    private static async Task EnsureAdminAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher)
    {
        const string adminRoleName = UserRoles.Admin;
        const string adminEmail = "admin@admin.ru";

        var adminRole = await context.Roles
            .SingleOrDefaultAsync(r => r.Name == adminRoleName);

        if (adminRole == null)
        {
            adminRole = new Role
            {
                Id = Guid.CreateVersion7(),
                Name = adminRoleName
            };
            context.Roles.Add(adminRole);
        }

        var adminUser = await context.Users
            .SingleOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            var salt = Guid.NewGuid().ToString();
            var password = "admin"; // только dev

            adminUser = new User
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                Email = adminEmail,
                JobTitle = "admin",
                Salt = salt,
                FullName = "Admin",
                PasswordHash = passwordHasher.HashPassword(password, salt)
            };

            context.Users.Add(adminUser);
        }

        var hasAdminRole = await context.UserRoles.AnyAsync(ur =>
            ur.UserId == adminUser.Id &&
            ur.RoleId == adminRole.Id);

        if (!hasAdminRole)
        {
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
        }
    }

    private static async Task EnsureStatusesAsync(AppDbContext context)
    {
        var requiredStatuses = new[]
        {
            "Создана",
            "В работе",
            "На согласовании",
            "Завершена"
        };

        var existing = await context.Statuses
            .Select(s => s.Name)
            .ToListAsync();

        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var statusesToAdd = requiredStatuses
            .Where(name => !existingSet.Contains(name))
            .Select(name => new Status
            {
                IsDefault = false,
                Name = name,
                TaskEntities = new List<TaskEntity>()
            });

        context.Statuses.AddRange(statusesToAdd);
    }

    private static bool IsTransient(Exception ex)
    {
        var t = ex;
        while (t != null)
        {
            var name = t.GetType().FullName ?? string.Empty;
            if (name.Contains("Npgsql") ||
                name.Contains("SocketException") ||
                name.Contains("TimeoutException"))
            {
                return true;
            }

            t = t.InnerException;
        }

        return false;
    }
}