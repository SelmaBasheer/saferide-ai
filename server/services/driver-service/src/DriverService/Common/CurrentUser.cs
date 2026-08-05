using System.Security.Claims;

namespace DriverService.Common;

public interface ICurrentUser
{
    Guid? SchoolId { get; } // null for SuperAdmin / anonymous / non-HTTP
}

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? SchoolId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("schoolId"), out var id)
            ? id
            : null;
}
