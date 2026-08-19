using Microsoft.AspNetCore.SignalR;
using SafeRide.Tracking.Hubs;

namespace SafeRide.Tracking.Startup;

public static class RealtimeExtensions
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSignalR();

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy
                    .WithOrigins(
                        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
            )
        );

        services.AddSingleton<IUserIdProvider, EmailUserIdProvider>();

        return services;
    }
}
