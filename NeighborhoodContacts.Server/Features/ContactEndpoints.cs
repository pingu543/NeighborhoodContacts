using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    // Endpoint registrations for contacts list
    public static class ContactEndpoints
    {
        public static void MapContactEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/contacts", GetAllContacts)
               .WithName("GetContacts")
               .WithTags("Contacts");

            app.MapGet("/api/property-groups", GetPropertyGroups)
               .RequireAuthorization("Admin")
               .WithName("GetPropertyGroups")
               .WithTags("Administration");
        }

        // Get contacts list for the contact list page.
        // Admins get all contacts; non-admins get only active and visible contacts.
        private static async Task<IResult> GetAllContacts(HttpContext http, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            // Check claim for admin role.
            var isAdmin = user?.Identity?.IsAuthenticated == true
                          && (user.IsInRole("Admin") || user.HasClaim(ClaimTypes.Role, "Admin"));

            // Build base query and apply non-admin filters.
            IQueryable<User> query = db.Users.AsNoTracking().Include(u => u.Property);

            Guid? requestedGroup = null;

            // Admin can accept an optional query param propertyGroupId
            if (isAdmin)
            {
                var q = http.Request.Query["propertyGroupId"].FirstOrDefault();
                if (Guid.TryParse(q, out var g)) requestedGroup = g;
            }
            else
            {
                // find current user's group
                var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();
                var currentUser = await db.Users
                                          .AsNoTracking()
                                          .Include(u => u.Property)
                                          .FirstOrDefaultAsync(u => u.Id == uid, ct);
                requestedGroup = currentUser?.Property?.PropertyGroupId;
                if (requestedGroup == null) return Results.Ok(new List<ContactListItemDto>()); // or Forbid
            }

            // apply filters
            if (!isAdmin) query = query.Where(u => u.IsActive && u.IsVisible);

            if (requestedGroup != null)
            {
                var rg = requestedGroup.Value;
                query = query.Where(u => u.Property != null && u.Property.PropertyGroupId == rg);
            }

            // Translate only the required fields.
            var list = await query
                .Select(u => new ContactListItemDto
                {
                    Id = u.Id,
                    ContactName = u.ContactName,
                    ContactNumber = u.ContactNumber,
                    ContactEmail = u.ContactEmail,
                    PropertyAddress = u.Property != null ? u.Property.Address : null
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

        // Admin-only: return all property groups
        // Authorization is enforced by the endpoint mapping.
        private static async Task<IResult> GetPropertyGroups(AppDbContext db, CancellationToken ct)
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
    }

    public sealed class ContactListItemDto
    {
        public Guid Id { get; init; }
        public string ContactName { get; init; } = string.Empty;
        public string? ContactNumber { get; init; }
        public string? ContactEmail { get; init; }
        public string? PropertyAddress { get; init; }
    }

    public sealed class PropertyGroupDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
