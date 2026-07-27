using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SafeRide.School.Api.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SafeRide.School"))
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation() // incoming HTTP requests
                    .AddHttpClientInstrumentation() // outgoing HTTP calls to other services
                    .AddConsoleExporter() // prints traces to console
            );

        return services;
    }
}
