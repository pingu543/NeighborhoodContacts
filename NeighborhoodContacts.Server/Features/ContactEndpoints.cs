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
        }

        // Get contacts list for the contact list page.
        // Admins get all contacts; non-admins get only active and visible contacts.
        private static async Task<IResult> GetAllContacts(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            // Check claim for admin role.
            var isAdmin = user?.Identity?.IsAuthenticated == true
                          && (user.IsInRole("Admin") || user.HasClaim(ClaimTypes.Role, "Admin"));

            // Build base query and apply non-admin filters.
            IQueryable<User> query = db.Users.AsNoTracking();

            if (!isAdmin)
            {
                query = query.Where(u => u.IsActive && u.IsVisible);
            }

            // Project only required fields (EF will translate navigation access in the projection without needing Include).
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
    }

    // DTO with only fields required by the contact list page
    public sealed class ContactListItemDto
    {
        public Guid Id { get; init; }
        public string ContactName { get; init; } = string.Empty;
        public string? ContactNumber { get; init; }
        public string? ContactEmail { get; init; }
        public string? PropertyAddress { get; init; }
    }
}
