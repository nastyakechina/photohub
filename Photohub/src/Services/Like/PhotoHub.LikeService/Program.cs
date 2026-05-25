using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Application.Commands;
using PhotoHub.LikeService.Application.Queries;
using PhotoHub.LikeService.Endpoints;
using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;
using PhotoHub.Observability;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

// CQRS Handlers
builder.Services.AddScoped<AddLikeCommandHandler>();
builder.Services.AddScoped<RemoveLikeCommandHandler>();
builder.Services.AddScoped<GetLikeCountQueryHandler>();
builder.Services.AddScoped<HasUserLikedQueryHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LikeDbContext>();
    db.Database.Migrate();
}

app.UsePhotoHubObservability();
app.UseCors();

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
