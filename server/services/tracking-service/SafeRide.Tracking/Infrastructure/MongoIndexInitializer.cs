using MongoDB.Driver;
using SafeRide.Tracking.Domain;

namespace SafeRide.Tracking.Infrastructure;

public sealed class MongoIndexInitializer(
    IMongoCollection<Trip> trips,
    IMongoCollection<GpsPoint> points,
    ILogger<MongoIndexInitializer> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await trips.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Trip>(
                    Builders<Trip>.IndexKeys.Ascending(t => t.SchoolId).Ascending(t => t.Status)
                ),
                new CreateIndexModel<Trip>(
                    Builders<Trip>.IndexKeys.Ascending(t => t.DriverId).Ascending(t => t.Status)
                ),
            ],
            cancellationToken
        );

        await points.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<GpsPoint>(
                    Builders<GpsPoint>
                        .IndexKeys.Ascending(p => p.TripId)
                        .Descending(p => p.RecordedAt)
                ),
                new CreateIndexModel<GpsPoint>(
                    Builders<GpsPoint>.IndexKeys.Ascending(p => p.RecordedAt),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(7) }
                ),
            ],
            cancellationToken
        );

        logger.LogInformation("Mongo indexes ensured");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
