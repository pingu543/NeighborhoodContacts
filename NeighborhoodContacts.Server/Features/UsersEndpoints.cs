using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;

namespace NeighborhoodContacts.Server.Features
{
    public static class UsersEndpoints
    {
        public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/users/me", GetCurrentUser)
               .RequireAuthorization()
               .WithName("GetCurrentUser")
               .WithTags("Users");

            app.MapGet("/api/users/{id:guid}", GetUserById)
               .RequireAuthorization()
               .WithName("GetUserById")
               .WithTags("Users");

            return app;
        }

        private static async Task<IResult> GetCurrentUser(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();

            var u = await db.Users
                        .AsNoTracking()
                        .Where(x => x.Id == uid)
                        .Select(x => new
                        {
                            x.Id,
                            x.Username,
                            x.ContactName,
                            x.ContactEmail,
                            x.ContactNumber,
                            PropertyAddress = x.Property != null ? x.Property.Address : null,
                            x.IsActive,
                            x.IsVisible,
                            x.IsAdmin
                        })
                        .FirstOrDefaultAsync(ct);

            if (u == null) return Results.NotFound();

            return Results.Ok(u);
        }

        private static async Task<IResult> GetUserById(Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            var isAdmin = user?.IsInRole("Admin") == true || user?.HasClaim(ClaimTypes.Role, "Admin") == true;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _ = Guid.TryParse(userIdClaim, out Guid currentUserId);
            var isOwner = currentUserId == id;

            if (!isAdmin && !isOwner)
            {
                var publicDto = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == id && u.IsVisible && u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        u.ContactName,
                        PropertyAddress = u.Property != null ? u.Property.Address : null
                    })
                    .FirstOrDefaultAsync(ct);

                return publicDto == null ? Results.NotFound() : Results.Ok(publicDto);
            }

            var detailed = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.ContactName,
                    u.ContactEmail,
                    u.ContactNumber,
                    PropertyAddress = u.Property != null ? u.Property.Address : null,
                    u.IsActive,
                    u.IsVisible,
                    u.IsAdmin
                })
                .FirstOrDefaultAsync(ct);

            if (detailed == null) return Results.NotFound();

            if (isAdmin) return Results.Ok(detailed);

            var ownerDto = new
            {
                detailed.Id,
                detailed.Username,
                detailed.ContactName,
                detailed.ContactEmail,
                detailed.ContactNumber,
                detailed.PropertyAddress
            };

            return Results.Ok(ownerDto);
        }
    }
}