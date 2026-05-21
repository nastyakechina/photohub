using PhotoHub.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddPhotoHubObservability("PhotoHub.ApiGateway");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UsePhotoHubObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("IncomingRequests");

    logger.LogInformation(
        "Incoming request {Method} {Path}",
        context.Request.Method,
        context.Request.Path);

    await next();
});

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
