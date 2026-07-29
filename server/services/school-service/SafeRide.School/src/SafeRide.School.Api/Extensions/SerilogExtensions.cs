using System.Globalization;
using Serilog;

namespace SafeRide.School.Api.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: "logs/SafeRide.School-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: CultureInfo.InvariantCulture
            )
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}
