using MongoDB.Driver;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Infrastructure;

namespace SafeRide.Tracking.Startup;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<MongoSettings>(configuration.GetSection("Mongo"));
        services.AddSingleton<MongoContext>();

        services.AddSingleton<IMongoCollection<Trip>>(sp =>
            sp.GetRequiredService<MongoContext>().Database.GetCollection<Trip>("trips")
        );

        services.AddSingleton<IMongoCollection<GpsPoint>>(sp =>
            sp.GetRequiredService<MongoContext>().Database.GetCollection<GpsPoint>("gps_points")
        );

        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }
}
