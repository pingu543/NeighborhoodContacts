using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeighborhoodContacts.Server.Data;
using System.Text;

namespace NeighborhoodContacts.Server.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure DbContext.
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")
                )
            );

            // Read JWT settings from configuration and validate them.
            var jwtOptions = GetRequiredJwtOptions(configuration);
            services.AddSingleton(jwtOptions);

            // Add authentication services and configure JWT bearer authentication.
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // Read token from cookie (AuthToken) so HttpOnly cookie auth works
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrEmpty(context.Token))
                            {
                                if (context.Request.Cookies.TryGetValue("AuthToken", out var token) && !string.IsNullOrEmpty(token))
                                {
                                    context.Token = token;
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Register authorization policy for admins
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            });

            return services;
        }

        // Helper method to read and validate JWT options from configuration.
        private static JwtOptions GetRequiredJwtOptions(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");

            var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
            var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
            var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var parsedMinutes) ? parsedMinutes : 60;

            return new JwtOptions
            {
                Issuer = issuer,
                Audience = audience,
                Key = key,
                ExpiresMinutes = expiresMinutes
            };
        }
    }
}
