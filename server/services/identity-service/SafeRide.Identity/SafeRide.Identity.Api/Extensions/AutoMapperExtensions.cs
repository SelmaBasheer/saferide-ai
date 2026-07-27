using SafeRide.Identity.Api.Mapping;

namespace SafeRide.Identity.Api.Extensions;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<AuthMappingProfile>();
        });
        return services;
    }
}
