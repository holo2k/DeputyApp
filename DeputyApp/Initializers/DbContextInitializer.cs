using DeputyApp.DAL;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.Initializers;

public static class DbContextInitializer
{
    public static void InitializeDatabase(this IServiceCollection services, string conn)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(conn));
    }

    public static async Task Migrate(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        await context.SaveChangesAsync();
    }
}