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
                ContactName = "Admin Admin",
                ContactNumber = "1234567890",
                ContactEmail = "admin@admin.com",
                IsActive = true,
                IsVisible = true,
                IsAdmin = true,
                Created = DateTime.UtcNow
            };

            db.Users.Add(admin);
            // also seed a non-admin user
            var userUsername = "user";
            var userPassword = "user";

            var userSalt = RandomNumberGenerator.GetBytes(16);
            var userHash = AuthEndpoints.HashPassword(userPassword, userSalt);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = userUsername,
                PasswordSalt = Convert.ToBase64String(userSalt),
                PasswordHash = Convert.ToBase64String(userHash),
                ContactName = "User User",
                ContactNumber = "0987654321",
                ContactEmail = "user@user.com",
                IsActive = true,
                IsVisible = true,
                IsAdmin = false,
                Created = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
    }
}