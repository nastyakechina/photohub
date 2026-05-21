using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Endpoints;
using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;
using PhotoHub.Observability;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.LikeService");

builder.Services.AddDbContext<LikeDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";

    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<LikeCounterCache>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UsePhotoHubObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

var likesGroup = app.MapGroup("/api/likes");

likesGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.LikeService",
    Status = "Healthy"
}));

likesGroup.MapLikeEndpoints();

app.Run();
