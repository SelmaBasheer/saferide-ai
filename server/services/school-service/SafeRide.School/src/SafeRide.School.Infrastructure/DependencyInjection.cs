using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.School.Application.Abstractions;
using SafeRide.School.Infrastructure.Messaging;

namespace SafeRide.School.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // ----- Messaging (RabbitMQ) -----
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName)
        );
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // ----- Persistence (EF Core) -----
        // Add your DbContext + provider, then uncomment the generic repo + unit of work:
        //
        //   services.AddDbContext<AppDbContext>(o =>
        //       o.UseNpgsql(configuration.GetConnectionString("Default")));   // or UseSqlServer / UseMySql
        //   services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
        //   services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        //   services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
