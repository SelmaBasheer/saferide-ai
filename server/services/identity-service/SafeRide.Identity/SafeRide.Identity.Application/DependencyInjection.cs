using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Password;
using SafeRide.Identity.Application.Auth.Refresh;
using SafeRide.Identity.Application.Auth.Register;
using SafeRide.Identity.Application.Auth.Verify;

namespace SafeRide.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterSchoolAdminCommandValidator>();
        services.AddScoped<RegisterSchoolAdminHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<RefreshTokenHandler>();

        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResendOtpHandler>();
        services.AddScoped<ResetPasswordHandler>();

        return services;
    }
}
