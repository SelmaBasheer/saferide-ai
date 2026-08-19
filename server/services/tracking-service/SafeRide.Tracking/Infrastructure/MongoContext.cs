using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace SafeRide.Tracking.Infrastructure;

public sealed class MongoContext
{
    static MongoContext()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public IMongoDatabase Database { get; }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var s = options.Value;

        var settings = new MongoClientSettings
        {
            Server = new MongoServerAddress(s.Host, s.Port),
            Credential = MongoCredential.CreateCredential(
                s.AuthenticationDatabase,
                s.Username,
                s.Password
            ),
        };

        Database = new MongoClient(settings).GetDatabase(s.Database);
    }
}
