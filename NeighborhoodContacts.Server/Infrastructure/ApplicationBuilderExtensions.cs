using NeighborhoodContacts.Server.Features;

namespace NeighborhoodContacts.Server.Infrastructure
{
    public static class ApplicationBuilderExtensions
    {
        // Setup HTTPS redirection, static files, authentication, and authorization.
        public static WebApplication UseApplicationPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();

            // Serve files from wwwroot (JS/CSS/index.html)
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        // Map API endpoints and SPA fallback
        public static WebApplication MapApplicationEndpoints(this WebApplication app)
        {
            app.MapAuthEndpoints();
            app.MapContactEndpoints();

            // Ensure client-side routing works
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}
