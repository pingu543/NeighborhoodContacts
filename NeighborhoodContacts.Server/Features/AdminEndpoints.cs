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

            app.MapPut("/api/admin/users/{id:guid}/property", UpdateUserProperty)
               .RequireAuthorization("Admin")
               .WithName("AdminUpdateUserProperty")
               .WithTags("Admin", "Users", "Properties");

            app.MapPut("/api/admin/users/{id:guid}/password", AdminSetUserPassword)
               .RequireAuthorization("Admin")
               .WithName("AdminSetUserPassword")
               .WithTags("Admin", "Users", "Password");

            app.MapGet("/api/admin/property-groups", GetPropertyGroupsAdmin)
               .RequireAuthorization("Admin")
               .WithName("GetAdminPropertyGroups")
               .WithTags("Admin", "PropertyGroups");

            app.MapPost("/api/admin/property-groups", CreatePropertyGroup)
               .RequireAuthorization("Admin")
               .WithName("AdminCreatePropertyGroup")
               .WithTags("Admin", "PropertyGroups");

            app.MapPut("/api/admin/property-groups/{id:guid}", UpdatePropertyGroup)
               .RequireAuthorization("Admin")
               .WithName("AdminUpdatePropertyGroup")
               .WithTags("Admin", "PropertyGroups");

            app.MapDelete("/api/admin/property-groups/{id:guid}", DeletePropertyGroup)
               .RequireAuthorization("Admin")
               .WithName("AdminDeletePropertyGroup")
               .WithTags("Admin", "PropertyGroups");

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

        // Admin sets a new password for another user.
        // No need to verify old password since admin wouldn't know it when helping user to reset.
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

        private sealed record SetPasswordRequest(string NewPassword);
        private sealed record SignUpRequest(string Username, string Password);
        private sealed record UpdateUserPropertyRequest(Guid? PropertyId);

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

        // Return all property groups
        private static async Task<IResult> GetPropertyGroupsAdmin(AppDbContext db, CancellationToken ct)
        {
            var groups = await db.PropertyGroups
                                 .AsNoTracking()
                                 .Select(pg => new PropertyGroupDto
                                 {
                                     Id = pg.Id,
                                     Name = pg.Name
                                 })
                                 .ToListAsync(ct);

            return Results.Ok(groups);
        }

        // Create property group
        private static async Task<IResult> CreatePropertyGroup(CreatePropertyGroupRequest request, AppDbContext db, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required." });

            var name = request.Name.Trim();
            if (name.Length < 1) return Results.BadRequest(new { error = "Name must be at least 1 character long." });

            var exists = await db.PropertyGroups.AnyAsync(pg => pg.Name == name, ct);
            if (exists) return Results.Conflict(new { error = "A property group with that name already exists." });

            var group = new PropertyGroup
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            db.PropertyGroups.Add(group);
            await db.SaveChangesAsync(ct);

            var dto = new PropertyGroupDto { Id = group.Id, Name = group.Name };
            return Results.Created($"/api/admin/property-groups/{group.Id}", dto);
        }

        // Update property group
        private static async Task<IResult> UpdatePropertyGroup(Guid id, UpdatePropertyGroupRequest request, AppDbContext db, CancellationToken ct)
        {
            var group = await db.PropertyGroups.FirstOrDefaultAsync(pg => pg.Id == id, ct);
            if (group == null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required." });

            var name = request.Name.Trim();
            if (name.Length < 1) return Results.BadRequest(new { error = "Name must be at least 1 character long." });

            var conflict = await db.PropertyGroups.AnyAsync(pg => pg.Name == name && pg.Id != id, ct);
            if (conflict) return Results.Conflict(new { error = "A property group with that name already exists." });

            group.Name = name;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        }

        // Delete property group
        private static async Task<IResult> DeletePropertyGroup(Guid id, AppDbContext db, CancellationToken ct)
        {
            var group = await db.PropertyGroups.FirstOrDefaultAsync(pg => pg.Id == id, ct);
            if (group == null) return Results.NotFound();

            var hasProperties = await db.Properties.AnyAsync(p => p.PropertyGroupId == id, ct);
            if (hasProperties)
                return Results.BadRequest(new { error = "Property group has properties and cannot be deleted." });

            db.PropertyGroups.Remove(group);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        }

        private sealed record CreatePropertyGroupRequest(string Name);
        private sealed record UpdatePropertyGroupRequest(string Name);
    }

    public sealed class PropertyGroupDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}