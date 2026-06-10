using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CheatsheetApp.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        // Check is intentionally non-atomic; app runs as a single instance.
        if (!await db.Users.AnyAsync())
        {
            var username = config["Admin:Username"]
                ?? throw new InvalidOperationException("Admin:Username is not configured.");
            var password = config["Admin:Password"]
                ?? throw new InvalidOperationException("Admin:Password is not configured.");

            db.Users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        if (config.GetValue("Database:SeedSampleData", false))
            await SeedData.ApplyAsync(db);
    }
}
