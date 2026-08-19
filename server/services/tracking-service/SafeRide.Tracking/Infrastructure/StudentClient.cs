using SafeRide.Tracking.Common;

namespace SafeRide.Tracking.Infrastructure;

public sealed record RosterEntryDto(
    Guid StudentId,
    string FirstName,
    string LastName,
    string ParentEmail,
    Guid PickupStopId
);

public sealed class StudentClient(HttpClient http)
{
    public async Task<List<RosterEntryDto>> GetRosterAsync(Guid routeId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/api/students/roster?routeId={routeId}", ct);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<RosterEntryDto>>>(
            ct
        );

        return envelope?.Data ?? [];
    }
}
