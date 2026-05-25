using Microsoft.Extensions.Http.Resilience;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Endpoints;
using PhotoHub.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.FeedService");

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
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
});

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

var feedGroup = app.MapGroup("/api/feed");

feedGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.FeedService",
    Status = "Healthy"
}));

feedGroup.MapFeedEndpoints();

app.Run();
