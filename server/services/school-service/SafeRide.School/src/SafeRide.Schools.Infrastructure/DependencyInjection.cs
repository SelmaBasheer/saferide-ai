using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Domain.Repositories;
using SafeRide.Schools.Infrastructure.Messaging;
using SafeRide.Schools.Infrastructure.Persistence;
using SafeRide.Schools.Infrastructure.Persistence.Repositories;

namespace SafeRide.Schools.Infrastructure;

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
        services.AddHostedService<IdentityEventsConsumer>();

        // ----- Persistence (EF Core) -----

        services.AddDbContext<SchoolDbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("Default"))
        );
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<SchoolDbContext>());
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
