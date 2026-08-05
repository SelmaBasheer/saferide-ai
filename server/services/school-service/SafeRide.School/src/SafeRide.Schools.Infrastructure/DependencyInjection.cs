using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Domain.Repositories;
using SafeRide.Schools.Infrastructure.Messaging;
using SafeRide.Schools.Infrastructure.Persistence;
using SafeRide.Schools.Infrastructure.Persistence.Repositories;
using SafeRide.Schools.Infrastructure.Storage;

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

        services.AddScoped<TenantStampInterceptor>();

        services.AddDbContext<SchoolDbContext>(
            (sp, options) =>
                options
                    .UseSqlServer(configuration.GetConnectionString("Default"))
                    .AddInterceptors(sp.GetRequiredService<TenantStampInterceptor>())
        );

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<SchoolDbContext>());
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
