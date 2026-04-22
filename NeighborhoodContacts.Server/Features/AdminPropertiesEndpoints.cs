using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminPropertiesEndpoints
    {
        public static IEndpointRouteBuilder MapAdminPropertyEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/admin/properties", GetPropertiesAdmin)
               .RequireAuthorization("Admin")
               .WithName("GetAdminProperties")
               .WithTags("Admin", "Properties");

            app.MapPost("/api/admin/properties", CreateProperty)
               .RequireAuthorization("Admin")
               .WithName("AdminCreateProperty")
               .WithTags("Admin", "Properties");

            app.MapPut("/api/admin/properties/{id:guid}", UpdateProperty)
               .RequireAuthorization("Admin")
               .WithName("AdminUpdateProperty")
               .WithTags("Admin", "Properties");

            app.MapDelete("/api/admin/properties/{id:guid}", DeleteProperty)
               .RequireAuthorization("Admin")
               .WithName("AdminDeleteProperty")
               .WithTags("Admin", "Properties");

            return app;
        }

        private static async Task<IResult> GetPropertiesAdmin(HttpContext http, AppDbContext db, CancellationToken ct)
        {
            Guid? requestedGroup = null;
            var q = http.Request.Query["propertyGroupId"].FirstOrDefault();
            if (Guid.TryParse(q, out var g)) requestedGroup = g;

            IQueryable<Property> query = db.Properties.AsNoTracking().Include(p => p.PropertyGroup);

            if (requestedGroup != null)
            {
                var rg = requestedGroup.Value;
                query = query.Where(p => p.PropertyGroupId == rg);
            }

            var list = await query
                .Select(p => new PropertyDto
                {
                    Id = p.Id,
                    Address = p.Address,
                    PropertyGroupId = p.PropertyGroupId,
                    PropertyGroupName = p.PropertyGroup != null ? p.PropertyGroup.Name : null
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

        private static async Task<IResult> CreateProperty(CreatePropertyRequest request, AppDbContext db, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Address))
                return Results.BadRequest(new { error = "Address is required." });

            if (request.PropertyGroupId == Guid.Empty)
                return Results.BadRequest(new { error = "PropertyGroupId is required." });

            var address = request.Address.Trim();
            if (address.Length < 1) return Results.BadRequest(new { error = "Address must be at least 1 character long." });

            var groupExists = await db.PropertyGroups.AnyAsync(pg => pg.Id == request.PropertyGroupId, ct);
            if (!groupExists) return Results.BadRequest(new { error = "Property group not found." });

            var exists = await db.Properties.AnyAsync(p => p.Address == address, ct);
            if (exists) return Results.Conflict(new { error = "A property with that address already exists." });

            var prop = new Property
            {
                Id = Guid.NewGuid(),
                Address = address,
                PropertyGroupId = request.PropertyGroupId
            };

            db.Properties.Add(prop);
            await db.SaveChangesAsync(ct);

            var dto = new PropertyDto
            {
                Id = prop.Id,
                Address = prop.Address,
                PropertyGroupId = prop.PropertyGroupId,
                PropertyGroupName = (await db.PropertyGroups.AsNoTracking().FirstOrDefaultAsync(pg => pg.Id == prop.PropertyGroupId, ct))?.Name
            };

            return Results.Created($"/api/admin/properties/{prop.Id}", dto);
        }

        private static async Task<IResult> UpdateProperty(Guid id, UpdatePropertyRequest request, AppDbContext db, CancellationToken ct)
        {
            var prop = await db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (prop == null) return Results.NotFound();

            if (request == null || string.IsNullOrWhiteSpace(request.Address))
                return Results.BadRequest(new { error = "Address is required." });

            if (request.PropertyGroupId == Guid.Empty)
                return Results.BadRequest(new { error = "PropertyGroupId is required." });

            var address = request.Address.Trim();
            if (address.Length < 1) return Results.BadRequest(new { error = "Address must be at least 1 character long." });

            var groupExists = await db.PropertyGroups.AnyAsync(pg => pg.Id == request.PropertyGroupId, ct);
            if (!groupExists) return Results.BadRequest(new { error = "Property group not found." });

            var conflict = await db.Properties.AnyAsync(p => p.Address == address && p.Id != id, ct);
            if (conflict) return Results.Conflict(new { error = "A property with that address already exists." });

            prop.Address = address;
            prop.PropertyGroupId = request.PropertyGroupId;

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        private static async Task<IResult> DeleteProperty(Guid id, AppDbContext db, CancellationToken ct)
        {
            var prop = await db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (prop == null) return Results.NotFound();

            var hasUsers = await db.Users.AnyAsync(u => u.PropertyId == id, ct);
            if (hasUsers)
                return Results.BadRequest(new { error = "Property has users and cannot be deleted." });

            db.Properties.Remove(prop);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        private sealed record CreatePropertyRequest(string Address, Guid PropertyGroupId);
        private sealed record UpdatePropertyRequest(string Address, Guid PropertyGroupId);

        public sealed class PropertyDto
        {
            public Guid Id { get; init; }
            public string Address { get; init; } = string.Empty;
            public Guid PropertyGroupId { get; init; }
            public string? PropertyGroupName { get; init; }
        }
    }
}