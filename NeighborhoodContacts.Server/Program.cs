using NeighborhoodContacts.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseApplicationPipeline();
app.MapApplicationEndpoints();

// seed admin if database empty
await app.SeedAdminIfEmptyAsync();

app.Run();
