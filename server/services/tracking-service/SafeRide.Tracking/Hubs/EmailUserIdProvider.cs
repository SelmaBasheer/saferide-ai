using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SafeRide.Tracking.Hubs;

public sealed class EmailUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.Email)?.ToLowerInvariant();
}
