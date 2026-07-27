namespace SafeRide.School.Api.Extensions;

public static class RouteExtensions
{
    public static IServiceCollection AddRouteOptions(this IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });
        return services;
    }
}
