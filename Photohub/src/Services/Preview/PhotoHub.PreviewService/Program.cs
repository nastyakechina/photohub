using MassTransit;
using PhotoHub.Observability;
using PhotoHub.PreviewService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.PreviewService");

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddConsumer<PhotoCreatedEventConsumer>();

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

app.UsePhotoHubObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.Run();
