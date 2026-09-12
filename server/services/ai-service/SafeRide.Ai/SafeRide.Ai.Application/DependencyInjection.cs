using Microsoft.Extensions.DependencyInjection;
using SafeRide.Ai.Application.Anomalies.Classify;
using SafeRide.Ai.Application.Anomalies.ListAlerts;
using SafeRide.Ai.Application.Anomalies.RecordDeviation;
using SafeRide.Ai.Application.Anomalies.ResolveAlert;

namespace SafeRide.Ai.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RecordDeviationHandler>();
        services.AddScoped<ClassifyAnomalyHandler>();
        services.AddScoped<ListAlertsHandler>();
        services.AddScoped<ResolveAlertHandler>();

        return services;
    }
}
