using NeighborhoodContacts.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.MapControllers();

// seed admin if database empty
await app.SeedAdminIfEmptyAsync();

app.Run();
