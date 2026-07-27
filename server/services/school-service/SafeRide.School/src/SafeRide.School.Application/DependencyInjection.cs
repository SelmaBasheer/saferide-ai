using Microsoft.Extensions.DependencyInjection;

namespace SafeRide.School.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register your handlers, validators, and use-cases here.
        return services;
    }
}
