using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using PhotoHub.FeedService.BackgroundServices;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Consumers;
using PhotoHub.FeedService.Endpoints;
using PhotoHub.FeedService.Infrastructure.Persistence;
using PhotoHub.FeedService.Services;
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

builder.AddPhotoHubObservability("PhotoHub.FeedService");

builder.Services.AddDbContext<FeedDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddHttpClient<FriendsServiceClient>(client =>
{
    var serviceUrl = builder.Configuration["Services:FriendsServiceUrl"]
        ?? "http://localhost:5002";
    client.BaseAddress = new Uri(serviceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
});

builder.Services.AddHttpClient<PhotoServiceClient>(client =>
{
    var serviceUrl = builder.Configuration["Services:PhotoServiceUrl"]
        ?? "http://localhost:5003";
    client.BaseAddress = new Uri(serviceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    var serviceUrl = builder.Configuration["Services:AuthServiceUrl"]
        ?? "http://localhost:5001";
    client.BaseAddress = new Uri(serviceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<RecommendationsService>();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumer<PhotoCreatedConsumer>();
    busConfigurator.AddConsumer<UserFollowedConsumer>();
    busConfigurator.AddConsumer<UserUnfollowedConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMq:Username"] ?? "photohub";
        var password = builder.Configuration["RabbitMq:Password"] ?? "photohub_password";

        configurator.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        configurator.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<FeedBackfillService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
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

var feedGroup = app.MapGroup("/api/feed");

feedGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.FeedService",
    Status = "Healthy"
}));

feedGroup.MapFeedEndpoints();

app.Run();
