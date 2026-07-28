using Microsoft.Extensions.DependencyInjection;
using SafeRide.Schools.Application.Schools.Command;

namespace SafeRide.Schools.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ApproveSchoolHandler>();
        services.AddScoped<GetSchoolsHandler>();
        return services;
    }
}
