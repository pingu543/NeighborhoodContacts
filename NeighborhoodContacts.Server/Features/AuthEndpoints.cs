using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;
using NeighborhoodContacts.Server.Infrastructure;

namespace NeighborhoodContacts.Server.Features
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/sign-in", SignIn)
               .AllowAnonymous()
               .WithName("SignIn")
               .WithTags("Authentication");

            app.MapPost("/api/auth/sign-out", (Delegate)SignOut)
               .AllowAnonymous()
               .WithName("SignOut")
               .WithTags("Authentication");

            // sign-up moved to AdminEndpoints
            return app;
        }

        // Sign in
        // Takes a username and password.
        // Creates a claim with the user's ID and role and issues a JWT token.
        // The token is set as an HttpOnly cookie.
        // Response body returns username and isAdmin.
        private static async Task<IResult> SignIn(SignInRequest request, AppDbContext dbContext, JwtOptions jwtOptions, HttpContext httpContext)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest(new { error = "Username is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Password is required." });
            }

            var username = request.Username.Trim();
            var password = request.Password.Trim();

            // Find the user by username and get the password hash, salt and active flag and admin flag.
            var dbUser = await dbContext.Users
                    .Where(u => u.Username == username)
                    .Select(u => new { u.Id, u.Username, u.PasswordHash, u.PasswordSalt, u.IsActive, u.IsAdmin })
                    .FirstOrDefaultAsync();
            if (dbUser == null)
            {
                return Results.BadRequest(new { error = "Invalid username or password." });
            }

            // Check whether the account is active
            if (!dbUser.IsActive)
            {
                return Results.Problem("Account is inactive. Please contact the administrator.", statusCode: 403);
            }

            // Verify the password
            var salt = Convert.FromBase64String(dbUser.PasswordSalt);
            var hash = HashPassword(password, salt);
            if (!hash.SequenceEqual(Convert.FromBase64String(dbUser.PasswordHash)))
            {
                return Results.BadRequest(new { error = "Invalid username or password." });
            }

            var claims = new List<Claim>
            {
                // The user's DB id is placed in the token claim for server-side identification
                new(ClaimTypes.NameIdentifier, dbUser.Id.ToString())
            };

            // Include role claim when user is admin
            if (dbUser.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                SecurityAlgorithms.HmacSha256
            );

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.ExpiresMinutes);

            var jwtToken = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // Set token as HttpOnly cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.None, // allow cross-site
                Path = "/",
                Expires = new DateTimeOffset(expiresAtUtc),
                IsEssential = true
            };

            httpContext.Response.Cookies.Append("AuthToken", token, cookieOptions);

            // Return username and admin flag
            return Results.Ok(new SignInResponse(dbUser.Username, dbUser.IsAdmin));
        }

        // Sign out
        // Removes the auth cookie so the client is signed out.
        private static async Task<IResult> SignOut(HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                IsEssential = true
            };

            // Ensure the cookie is cleared with the same attributes used when setting it.
            httpContext.Response.Cookies.Delete("AuthToken", cookieOptions);

            return Results.Ok();
        }

        public static byte[] HashPassword(string password, byte[] salt)
        {
            const int iterations = 100000;
            const int hashByteSize = 32;
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashByteSize);
        }

        private sealed record SignInRequest(string Username, string Password);
        private sealed record SignInResponse(string Username, bool IsAdmin);
    }
}
