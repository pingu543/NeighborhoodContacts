using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;

namespace NeighborhoodContacts.Server.Features
{
    public static class PropertyEndpoints
    {
        public static void MapPropertyEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/property-groups", GetPropertyGroups)
               .RequireAuthorization("Admin")
               .WithName("GetPropertyGroups")
               .WithTags("Administration");
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

    public sealed class PropertyGroupDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}