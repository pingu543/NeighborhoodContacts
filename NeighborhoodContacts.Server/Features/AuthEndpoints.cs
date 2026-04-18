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
            app.MapPost("/auth/sign-in", SignIn)
               .AllowAnonymous()
               .WithName("SignIn")
               .WithTags("Authentication");

            // Require the "Admin" policy (defined in dependency injection) to create accounts.
            app.MapPost("/auth/sign-up", SignUp)
               .RequireAuthorization("Admin")
               .WithName("SignUp")
               .WithTags("Authentication");

            return app;
        }

        // Sign in
        // Takes a username and password.
        // Creates a claim with the user's ID and role (if admin) and returns a JWT token containing those claims.
        private static async Task<IResult> SignIn(SignInRequest request, AppDbContext dbContext, JwtOptions jwtOptions)
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
            var user = await dbContext.Users
                    .Where(u => u.Username == username)
                    .Select(u => new { u.Id, u.PasswordHash, u.PasswordSalt, u.IsActive, u.IsAdmin })
                    .FirstOrDefaultAsync();
            if (user == null)
            {
                return Results.BadRequest(new { error = "Invalid username or password." });
            }

            // Check whether the account is active
            if (!user.IsActive)
            {
                return Results.Problem("Account is inactive. Please contact the administrator.", statusCode: 403);
            }

            // Verify the password
            var salt = Convert.FromBase64String(user.PasswordSalt);
            var hash = HashPassword(password, salt);
            if (!hash.SequenceEqual(Convert.FromBase64String(user.PasswordHash)))
            {
                return Results.BadRequest(new { error = "Invalid username or password." });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Include role claim when user is admin
            if (user.IsAdmin)
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

            return Results.Ok(new SignInResponse(token));
        }

        // Create account (admin-only)
        // Admin creates accounts for users. Authorization policy ensures caller is admin, so no DB admin check here.
        private static async Task<IResult> SignUp(SignUpRequest request, AppDbContext dbContext, ClaimsPrincipal user)
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

            if (username.Length < 3)
            {
                return Results.BadRequest(new { error = "Username must be at least 3 characters long." });
            }

            if (password.Length < 8)
            {
                return Results.BadRequest(new { error = "Password must be at least 8 characters long." });
            }

            var exists = await dbContext.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                return Results.Conflict(new { error = "Username is already taken." });
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = HashPassword(password, salt);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                IsActive = true,
                IsVisible = true,
                IsAdmin = false,
                Created = DateTime.UtcNow
            };

            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/users/{newUser.Id}", new SignUpResponse(newUser.Id));
        }

        public static byte[] HashPassword(string password, byte[] salt)
        {
            const int iterations = 100000;
            const int hashByteSize = 32;
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashByteSize);
        }

        private sealed record SignInRequest(string Username, string Password);
        private sealed record SignInResponse(string Token);

        private sealed record SignUpRequest(string Username, string Password);
        private sealed record SignUpResponse(Guid Id);
    }
}
