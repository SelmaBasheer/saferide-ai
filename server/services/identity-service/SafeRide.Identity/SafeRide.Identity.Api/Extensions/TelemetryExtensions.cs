using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SafeRide.Identity.Api.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SafeRide.Identity"))
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation() // traces incoming HTTP requests
                    .AddHttpClientInstrumentation() // traces outgoing HTTP calls to other services
                    .AddConsoleExporter()
            ); // prints traces to console

        return services;
    }
}
