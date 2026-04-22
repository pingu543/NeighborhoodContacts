using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;

namespace NeighborhoodContacts.Server.Features
{
    public static class PropertyEndpoints
    {
        public static void MapPropertyEndpoints(this IEndpointRouteBuilder app)
        {
            // Authenticated users: get the name of the property group for the caller's property
            app.MapGet("/api/property-group/me", GetMyPropertyGroupName)
               .RequireAuthorization()
               .WithName("GetMyPropertyGroup")
               .WithTags("Property");
        }

        // Return the name of the property group for the authenticated user's property.
        //  - 200: propertyGroupName: your group name
        //  - 404: no property or no property group for this user
        //  - 403: missing/invalid user id claim
        private static async Task<IResult> GetMyPropertyGroupName(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();

            var name = await db.Users
                               .AsNoTracking()
                               .Where(u => u.Id == uid)
                               .Select(u => u.Property != null && u.Property.PropertyGroup != null ? u.Property.PropertyGroup.Name : null)
                               .FirstOrDefaultAsync(ct);

            return name == null ? Results.NotFound() : Results.Ok(new { propertyGroupName = name });
        }
    }
}