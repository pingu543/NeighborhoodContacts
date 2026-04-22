using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using System.Security.Cryptography;

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

            app.MapPut("/api/users/me/password", ChangeMyPassword)
               .RequireAuthorization()
               .WithName("ChangeMyPassword")
               .WithTags("Users, Password");

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

        // Allow authenticated user to change their own password.
        // Requires providing the current password.
        private static async Task<IResult> ChangeMyPassword(ClaimsPrincipal user, ChangePasswordRequest request, AppDbContext db, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "CurrentPassword and NewPassword are required." });

            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var uid)) return Results.Forbid();

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == uid, ct);
            if (dbUser == null) return Results.NotFound();

            // Verify current password
            var storedSalt = Convert.FromBase64String(dbUser.PasswordSalt);
            var storedHash = Convert.FromBase64String(dbUser.PasswordHash);
            var suppliedHash = AuthEndpoints.HashPassword(request.CurrentPassword.Trim(), storedSalt);
            if (!suppliedHash.SequenceEqual(storedHash))
            {
                return Results.BadRequest(new { error = "Current password is incorrect." });
            }

            // Update to new password
            var newSalt = RandomNumberGenerator.GetBytes(16);
            var newHash = AuthEndpoints.HashPassword(request.NewPassword.Trim(), newSalt);

            dbUser.PasswordSalt = Convert.ToBase64String(newSalt);
            dbUser.PasswordHash = Convert.ToBase64String(newHash);

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    }
}