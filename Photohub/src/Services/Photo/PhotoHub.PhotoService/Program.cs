using Amazon.S3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PhotoHub.Observability;
using PhotoHub.PhotoService.Endpoints;
using PhotoHub.PhotoService.Infrastructure.Persistence;

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

builder.AddPhotoHubObservability("PhotoHub.PhotoService");

builder.Services.AddDbContext<PhotoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        var rabbitMqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitMqUsername = builder.Configuration["RabbitMq:Username"] ?? "photohub";
        var rabbitMqPassword = builder.Configuration["RabbitMq:Password"] ?? "photohub_password";

        configurator.Host(rabbitMqHost, "/", hostConfigurator =>
        {
            hostConfigurator.Username(rabbitMqUsername);
            hostConfigurator.Password(rabbitMqPassword);
        });

        configurator.ConfigureEndpoints(context);
    });
});

var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost";
var minioPort     = builder.Configuration["MinIO:Port"]     ?? "9000";
var minioKey      = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
var minioSecret   = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin123";

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(
    minioKey, minioSecret,
    new AmazonS3Config
    {
        ServiceURL = $"http://{minioEndpoint}:{minioPort}",
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1",
    }
));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PhotoDbContext>();
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

var photosGroup = app.MapGroup("/api/photos");

photosGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.PhotoService",
    Status = "Healthy"
}));

photosGroup.MapPhotoEndpoints();

app.Run();
