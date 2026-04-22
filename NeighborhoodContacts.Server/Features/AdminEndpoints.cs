using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminEndpoints
    {
        public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/contacts", GetAllContactsAdmin)
               .RequireAuthorization("Admin")
               .WithName("GetAdminContacts")
               .WithTags("Admin", "Contacts");

            app.MapPost("/api/admin/users", CreateUser)
               .RequireAuthorization("Admin")
               .WithName("AdminCreateUser")
               .WithTags("Admin", "Users");

            return app;
        }

        private static async Task<IResult> GetAllContactsAdmin(HttpContext http, AppDbContext db, CancellationToken ct)
        {
            Guid? requestedGroup = null;
            var q = http.Request.Query["propertyGroupId"].FirstOrDefault();
            if (Guid.TryParse(q, out var g)) requestedGroup = g;

            IQueryable<User> query = db.Users.AsNoTracking().Include(u => u.Property);

            if (requestedGroup != null)
            {
                var rg = requestedGroup.Value;
                query = query.Where(u => u.Property != null && u.Property.PropertyGroupId == rg);
            }

            var list = await query
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ContactName,
                    u.ContactNumber,
                    u.ContactEmail,
                    PropertyAddress = u.Property != null ? u.Property.Address : null,
                    u.IsActive,
                    u.IsVisible
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

        private static async Task<IResult> CreateUser(SignUpRequest request, AppDbContext db)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
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

        private sealed record SignUpRequest(string Username, string Password);
    }
}