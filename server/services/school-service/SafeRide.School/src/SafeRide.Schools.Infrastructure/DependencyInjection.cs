using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Domain.Repositories;
using SafeRide.Schools.Infrastructure.Jobs;
using SafeRide.Schools.Infrastructure.Messaging;
using SafeRide.Schools.Infrastructure.Payments;
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

        // ----- Razorpay Payment Gateway -----
        services.Configure<RazorpaySettings>(
            configuration.GetSection(RazorpaySettings.SectionName)
        );

        services.AddHttpClient<IPaymentGateway, RazorpayGateway>(
            (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<RazorpaySettings>>().Value;

                client.BaseAddress = new Uri("https://api.razorpay.com/");

                // Basic auth with the key id as username and the secret as
                // password — Razorpay's scheme, and the reason the secret never
                // leaves the server.
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{settings.KeyId}:{settings.KeySecret}")
                );

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }
        );

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<SchoolDbContext>());
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddHostedService<SubscriptionExpiryWorker>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
