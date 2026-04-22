using Microsoft.AspNetCore.Builder;

namespace NeighborhoodContacts.Server.Features
{
    public static class AdminEndpoints
    {
        public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            // Register smaller feature endpoint groups
            app.MapAdminUserEndpoints();
            app.MapAdminContactEndpoints();
            app.MapAdminPropertyGroupEndpoints();
            app.MapAdminPropertyEndpoints();

            return app;
        }
    }
}