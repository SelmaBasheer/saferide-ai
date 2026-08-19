using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SafeRide.Tracking.Infrastructure;
using SafeRide.Tracking.Jobs;

namespace SafeRide.Tracking.Startup;

public static class JobExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddScoped<DeviationCheckJob>();

        services.AddHangfire(
            (provider, config) =>
            {
                var settings = provider.GetRequiredService<IOptions<MongoSettings>>().Value;

                var clientSettings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(settings.Host, settings.Port),
                    Credential = MongoCredential.CreateCredential(
                        settings.AuthenticationDatabase,
                        settings.Username,
                        settings.Password
                    ),
                };

                config.UseMongoStorage(
                    clientSettings,
                    settings.Database,
                    new MongoStorageOptions
                    {
                        MigrationOptions = new MongoMigrationOptions
                        {
                            MigrationStrategy = new MigrateMongoMigrationStrategy(),
                            BackupStrategy = new CollectionMongoBackupStrategy(),
                        },
                        CheckConnection = false,
                    }
                );
            }
        );

        services.AddHangfireServer();

        return services;
    }
}
