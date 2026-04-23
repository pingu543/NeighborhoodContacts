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

            // also seed another non-admin user
            var userUsername2 = "user2";
            var userPassword2 = "user2";

            var userSalt2 = RandomNumberGenerator.GetBytes(16);
            var userHash2 = AuthEndpoints.HashPassword(userPassword2, userSalt2);
            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Username = userUsername2,
                PasswordSalt = Convert.ToBase64String(userSalt2),
                PasswordHash = Convert.ToBase64String(userHash2),
                ContactName = "User II",
                ContactNumber = "0987654321",
                ContactEmail = "user2@user.com",
                IsActive = true,
                IsVisible = true,
                IsAdmin = false,
                Created = DateTime.UtcNow
            };

            db.Users.Add(user2);

            // seed a property group with two properties
            var propertyGroup = new NeighborhoodContacts.Server.Data.Entities.PropertyGroup
            {
                Id = Guid.NewGuid(),
                Name = "Default Group",
                Properties =
                [
                    new() {
                        Id = Guid.NewGuid(),
                        Address = "123 Demo St"
                    },
                    new() {
                        Id = Guid.NewGuid(),
                        Address = "456 Test Ave"
                    }
                ]
            };

            db.PropertyGroups.Add(propertyGroup);

            await db.SaveChangesAsync();
        }
    }
}