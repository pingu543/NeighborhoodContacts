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
                    IsVisible = u.IsVisible
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

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