using SafeRide.Tracking.Infrastructure;
using SafeRide.Tracking.Infrastructure.Messaging;

namespace SafeRide.Tracking.Startup;

public static class IntegrationExtensions
{
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthHandler>();

        services
            .AddHttpClient<RouteClient>(c =>
                c.BaseAddress = new Uri(configuration["Services:Route"]!)
            )
            .AddHttpMessageHandler<ForwardAuthHandler>();

        services
            .AddHttpClient<StudentClient>(c =>
                c.BaseAddress = new Uri(configuration["Services:Student"]!)
            )
            .AddHttpMessageHandler<ForwardAuthHandler>();

        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}
