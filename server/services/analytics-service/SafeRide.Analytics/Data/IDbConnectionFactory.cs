using System.Data;

namespace SafeRide.Analytics.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
