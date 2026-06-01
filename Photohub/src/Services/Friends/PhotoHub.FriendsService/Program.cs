using MassTransit;
using Microsoft.EntityFrameworkCore;
using PhotoHub.FriendsService.Endpoints;
using PhotoHub.FriendsService.Infrastructure.Persistence;
using PhotoHub.Observability;

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

builder.AddPhotoHubObservability("PhotoHub.FriendsService");

builder.Services.AddDbContext<FriendsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddMassTransit(busConfigurator =>
{
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FriendsDbContext>();
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

var friendsGroup = app.MapGroup("/api/friends");

friendsGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.FriendsService",
    Status = "Healthy"
}));

friendsGroup.MapFriendsEndpoints();

app.Run();
