using MassTransit;
using Microsoft.EntityFrameworkCore;
using PhotoHub.Observability;
using PhotoHub.PhotoService.Endpoints;
using PhotoHub.PhotoService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PhotoDbContext>();
    db.Database.Migrate();
}

app.UsePhotoHubObservability();

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
