namespace SafeRide.Ai.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Alerts concern a school's operations, so only its admins see them.
            options.AddPolicy("SchoolAdmin", policy => policy.RequireRole("SchoolAdmin"));

            // Dead letters belong to the deployment, not to a school, so this policy
            // deliberately requires no schoolId — SuperAdmin tokens don't carry one.
            options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
        });

        return services;
    }
}
