using System.Security.Claims;
using SafeRide.Analytics.Abstractions;

namespace SafeRide.Analytics.Common;

public sealed class HttpContextTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    public Guid? SchoolId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue("schoolId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
