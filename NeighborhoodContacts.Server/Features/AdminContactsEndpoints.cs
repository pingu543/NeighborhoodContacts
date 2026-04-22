using Azure.Core;
using Microsoft.EntityFrameworkCore;
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

            var username = request.Username.Trim();
            if (username.Length < 1) return Results.BadRequest(new { error = "Name must be at least 1 character long." });

            var exists = await db.Users.AnyAsync(u => u.Username == username, ct);
            if (exists) return Results.Conflict(new { error = "A contact with that username already exists." });

            var property = await db.Properties.Where(p => p.Address == request.ContactAddress).FirstAsync();
            if (property == null) return Results.Conflict(new { error = "The contacts property doesn't exist" });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
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

        private sealed record CreateContactRequest(string Username, string ContactName, string ContactEmail, string ContactNumber, string ContactAddress);

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