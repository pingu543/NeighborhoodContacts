using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using NeighborhoodContacts.Server.Features;

namespace NeighborhoodContacts.Server.Infrastructure
{
    public static class ApplicationBuilderExtensions
    {
        // Configure the HTTP pipeline in one place.
        public static WebApplication UseApplicationPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Serve SPA static files from wwwroot (JS/CSS/index.html)
            app.UseStaticFiles();

            // Routing must be added before auth middleware when using endpoint routing.
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        // Map feature endpoint groups and SPA fallback from a single place.
        public static WebApplication MapApplicationEndpoints(this WebApplication app)
        {
            app.MapAuthEndpoints();
            app.MapUsersEndpoints();
            app.MapAdminEndpoints();
            app.MapContactEndpoints();
            app.MapPropertyEndpoints();

            // SPA fallback for client-side routing (serve index.html)
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}
