using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminPropertyGroupsEndpoints
    {
        public static IEndpointRouteBuilder MapAdminPropertyGroupEndpoints(this IEndpointRouteBuilder app)
        {
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

        private static async Task<IResult> CreatePropertyGroup(CreatePropertyGroupRequest request, AppDbContext db, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
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

        private static async Task<IResult> UpdatePropertyGroup(Guid id, UpdatePropertyGroupRequest request, AppDbContext db, CancellationToken ct)
        {
            var group = await db.PropertyGroups.FirstOrDefaultAsync(pg => pg.Id == id, ct);
            if (group == null) return Results.NotFound();

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required." });

            var name = request.Name.Trim();
            if (name.Length < 1) return Results.BadRequest(new { error = "Name must be at least 1 character long." });

            var conflict = await db.PropertyGroups.AnyAsync(pg => pg.Name == name && pg.Id != id, ct);
            if (conflict) return Results.Conflict(new { error = "A property group with that name already exists." });

            group.Name = name;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        }

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