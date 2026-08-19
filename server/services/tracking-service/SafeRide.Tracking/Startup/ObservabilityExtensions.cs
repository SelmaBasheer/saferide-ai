using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace SafeRide.Tracking.Startup;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName
    )
    {
        builder.Host.UseSerilog(
            (context, config) =>
                config
                    .MinimumLevel.Information()
                    .MinimumLevel.Override(
                        "Microsoft.AspNetCore",
                        Serilog.Events.LogEventLevel.Warning
                    )
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProperty("service", serviceName)
                    .WriteTo.Console(new CompactJsonFormatter())
        );

        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t =>
                t.AddAspNetCoreInstrumentation(o => o.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter()
            );

        return builder;
    }
}
