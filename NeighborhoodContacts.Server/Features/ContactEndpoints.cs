using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    // Endpoint registrations for contacts list (user-scoped)
    public static class ContactEndpoints
    {
        public static void MapContactEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/contacts", GetAllContacts)
               .WithName("GetContacts")
               .WithTags("Contacts");
        }

        // Get contacts list for the contact list page (user view).
        // Non-admin callers get only active and visible contacts in their property group.
        private static async Task<IResult> GetAllContacts(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            // find current user's group
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();

            var currentUser = await db.Users
                                      .AsNoTracking()
                                      .Include(u => u.Property)
                                      .FirstOrDefaultAsync(u => u.Id == uid, ct);

            var requestedGroup = currentUser?.Property?.PropertyGroupId;
            if (requestedGroup == null) return Results.Ok(new List<ContactListItemDto>());

            var rg = requestedGroup.Value;

            var list = await db.Users
                .AsNoTracking()
                .Include(u => u.Property)
                .Where(u => u.IsActive && u.IsVisible && u.Property != null && u.Property.PropertyGroupId == rg)
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

    public sealed class ContactListItemDto
    {
        public Guid Id { get; init; }
        public string ContactName { get; init; } = string.Empty;
        public string? ContactNumber { get; init; }
        public string? ContactEmail { get; init; }
        public string? PropertyAddress { get; init; }
    }
}
