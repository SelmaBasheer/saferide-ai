using Serilog;
using Serilog.Formatting.Compact;

namespace SafeRide.Ai.Api.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Structured JSON to stdout, never to files — the container is the log source.
        builder.Host.UseSerilog(
            (context, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new CompactJsonFormatter())
        );

        return builder;
    }
}
