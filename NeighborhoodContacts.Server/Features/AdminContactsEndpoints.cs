using Azure.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminContactsEndpoints
    {
        public static IEndpointRouteBuilder MapAdminContactEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/contacts", GetAllContactsAdmin)
               .RequireAuthorization("Admin")
               .WithName("GetAdminContacts")
               .WithTags("Admin", "Contacts");
            app.MapPost("/api/admin/contacts", CreateContact)
               .RequireAuthorization("Admin")
               .WithName("AdminCreateContact")
               .WithTags("Admin", "Contacts");
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
                .Select(u => new AdminContactDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ContactName = u.ContactName,
                    ContactNumber = u.ContactNumber,
                    ContactEmail = u.ContactEmail,
                    PropertyAddress = u.Property != null ? u.Property.Address : null,
                    IsActive = u.IsActive,
                    IsVisible = u.IsVisible,
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

        private static async Task<IResult> CreateContact(CreateContactRequest request, AppDbContext db, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
                return Results.BadRequest(new { error = "Username is required." });

            if (string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Password is required." });

            var username = request.Username.Trim();
            var password = request.Password.Trim();

            if (username.Length < 1) return Results.BadRequest(new { error = "Name must be at least 1 character long." });
            if (password.Length < 1) return Results.BadRequest(new { error = "Password must be at least 1 character long." });

            var exists = await db.Users.AnyAsync(u => u.Username == username, ct);
            if (exists) return Results.Conflict(new { error = "A contact with that username already exists." });

            if (request.PropertyId == null || request.PropertyId == Guid.Empty)
                return Results.BadRequest(new { error = "PropertyId is required." });

            var property = await db.Properties.FindAsync(new object[] { request.PropertyId.Value }, ct);
            if (property == null) return Results.Conflict(new { error = "The selected property doesn't exist." });

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = AuthEndpoints.HashPassword(password, salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                ContactName = request.ContactName,
                ContactNumber = request.ContactNumber,
                ContactEmail = request.ContactEmail,
                PropertyId = property.Id,
                IsActive = true,
                IsVisible = true,
                Created = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            var dto = new PropertyGroupDto { Id = user.Id, Name = user.Username };
            return Results.Created($"/api/admin/contacts/{user.Id}", dto);
        }

        private sealed record CreateContactRequest(
            string Username,
            string Password,
            string ContactName,
            string ContactEmail,
            string ContactNumber,
            Guid? PropertyId
        );

        public sealed class AdminContactDto
        {
            public Guid Id { get; init; }
            public string Username { get; init; } = string.Empty;
            public string ContactName { get; init; } = string.Empty;
            public string? ContactNumber { get; init; }
            public string? ContactEmail { get; init; }
            public string? PropertyAddress { get; init; }
            public bool IsActive { get; init; }
            public bool IsVisible { get; init; }
        }
    }
}