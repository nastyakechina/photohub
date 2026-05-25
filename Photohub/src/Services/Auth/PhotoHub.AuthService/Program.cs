using Microsoft.EntityFrameworkCore;
using PhotoHub.AuthService.Application;
using PhotoHub.AuthService.Endpoints;
using PhotoHub.AuthService.Infrastructure.Persistence;
using PhotoHub.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.AuthService");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

app.UsePhotoHubObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

var authGroup = app.MapGroup("/api/auth");

authGroup.MapGet("/health", () => Results.Ok(new
{
    Service = "PhotoHub.AuthService",
    Status = "Healthy"
}));

authGroup.MapAuthEndpoints();

app.Run();
