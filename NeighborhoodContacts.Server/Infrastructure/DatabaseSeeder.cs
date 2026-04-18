using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;
using NeighborhoodContacts.Server.Features;

namespace NeighborhoodContacts.Server.Infrastructure
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAdminIfEmptyAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var config = services.GetRequiredService<IConfiguration>();
            var db = services.GetRequiredService<AppDbContext>();

            // Apply migrations if you want automatic schema updates at startup
            await db.Database.MigrateAsync();

            if (await db.Users.AnyAsync())
                return;

            var adminUsername = config["Admin:Username"] ?? "admin";
            var adminPassword = config["Admin:Password"] ?? "admin";

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = AuthEndpoints.HashPassword(adminPassword, salt);
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Username = adminUsername,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                IsActive = true,
                IsVisible = true,
                IsAdmin = true,
                Created = DateTime.UtcNow
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}