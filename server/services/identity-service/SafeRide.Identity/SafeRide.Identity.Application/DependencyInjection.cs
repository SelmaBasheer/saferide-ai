using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Refresh;
using SafeRide.Identity.Application.Auth.Register;

namespace SafeRide.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterSchoolAdminCommandValidator>();
        services.AddScoped<RegisterSchoolAdminHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();

        return services;
    }
}
