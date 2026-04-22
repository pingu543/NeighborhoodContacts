using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminUsersEndpoints
    {
        public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/admin/users", CreateUser)
               .RequireAuthorization("Admin")
               .WithName("AdminCreateUser")
               .WithTags("Admin", "Users");

            app.MapPut("/api/admin/users/{id:guid}/property", UpdateUserProperty)
               .RequireAuthorization("Admin")
               .WithName("AdminUpdateUserProperty")
               .WithTags("Admin", "Users", "Properties");

            app.MapPut("/api/admin/users/{id:guid}/password", AdminSetUserPassword)
               .RequireAuthorization("Admin")
               .WithName("AdminSetUserPassword")
               .WithTags("Admin", "Users", "Password");

            return app;
        }

        private static async Task<IResult> CreateUser(SignUpRequest request, AppDbContext db)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Username and password are required." });

            var username = request.Username.Trim();
            var password = request.Password.Trim();

            if (username.Length < 1) return Results.BadRequest(new { error = "Username must be at least 1 character long." });
            if (password.Length < 1) return Results.BadRequest(new { error = "Password must be at least 1 character long." });

            var exists = await db.Users.AnyAsync(u => u.Username == username);
            if (exists) return Results.Conflict(new { error = "Username is already taken." });

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = AuthEndpoints.HashPassword(password, salt);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                IsActive = true,
                IsVisible = true,
                IsAdmin = false,
                Created = DateTime.UtcNow
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{newUser.Id}", null);
        }

        private static async Task<IResult> AdminSetUserPassword(Guid id, SetPasswordRequest request, AppDbContext db, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "NewPassword is required." });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null) return Results.NotFound();

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = AuthEndpoints.HashPassword(request.NewPassword.Trim(), salt);

            user.PasswordSalt = Convert.ToBase64String(salt);
            user.PasswordHash = Convert.ToBase64String(hash);

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        private static async Task<IResult> UpdateUserProperty(Guid id, UpdateUserPropertyRequest request, AppDbContext db, CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null) return Results.NotFound();

            if (request.PropertyId == null)
            {
                user.PropertyId = null;
            }
            else
            {
                var propExists = await db.Properties.AnyAsync(p => p.Id == request.PropertyId.Value, ct);
                if (!propExists) return Results.BadRequest(new { error = "Property not found." });

                user.PropertyId = request.PropertyId;
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        private sealed record SetPasswordRequest(string NewPassword);
        private sealed record SignUpRequest(string Username, string Password);
        private sealed record UpdateUserPropertyRequest(Guid? PropertyId);
    }
}