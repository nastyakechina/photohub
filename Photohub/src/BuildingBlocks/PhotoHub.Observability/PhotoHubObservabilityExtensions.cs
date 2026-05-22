using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PhotoHub.Observability;

public static class PhotoHubObservabilityExtensions
{
    public static WebApplicationBuilder AddPhotoHubObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });

        builder.Services.AddSingleton(new PhotoHubObservabilityOptions(serviceName));
        builder.Services.AddHealthChecks();

        return builder;
    }

    public static WebApplication UsePhotoHubObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        return app;
    }
}
