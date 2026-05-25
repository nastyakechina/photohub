using Microsoft.EntityFrameworkCore;
using PhotoHub.FriendsService.Endpoints;
using PhotoHub.FriendsService.Infrastructure.Persistence;
using PhotoHub.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.FriendsService");

builder.Services.AddDbContext<FriendsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FriendsDbContext>();
    db.Database.Migrate();
}

app.UsePhotoHubObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

var friendsGroup = app.MapGroup("/api/friends");

friendsGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.FriendsService",
    Status = "Healthy"
}));

friendsGroup.MapFriendsEndpoints();

app.Run();
