using System.Net;
using SafeRide.Tracking.Common;

namespace SafeRide.Tracking.Infrastructure;

public sealed record RouteStopDto(
    Guid StopId,
    int Sequence,
    string Name,
    double Latitude,
    double Longitude,
    string PickupTime
);

public sealed record GeoPointDto(double Latitude, double Longitude);

public sealed record RouteDto(
    Guid Id,
    Guid SchoolId,
    string Code,
    string Name,
    string Status,
    Guid? AssignedBusId,
    List<RouteStopDto> Stops,
    List<GeoPointDto>? Path
);

public sealed class RouteClient(HttpClient http)
{
    public async Task<RouteDto> GetRouteAsync(Guid routeId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/api/routes/{routeId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw AppException.NotFound("Route not found.");
        }

        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(ct);

        return envelope?.Data ?? throw AppException.NotFound("Route not found.");
    }
}
