using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

namespace PhotoHub.Observability;

public static class PhotoHubObservabilityExtensions
{
    public static WebApplicationBuilder AddPhotoHubObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        builder.Host.UseSerilog();

        var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
            });

        builder.Services.AddSingleton(new PhotoHubObservabilityOptions(serviceName));
        builder.Services.AddHealthChecks();

        return builder;
    }

    public static WebApplication UsePhotoHubObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseHttpMetrics();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapMetrics("/metrics");

        return app;
    }
}
