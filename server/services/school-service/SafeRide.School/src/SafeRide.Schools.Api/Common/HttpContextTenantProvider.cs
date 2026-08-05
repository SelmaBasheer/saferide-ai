using System.Security.Claims;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Api.Common;

public sealed class HttpContextTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    public Guid? TenantId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue("schoolId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
