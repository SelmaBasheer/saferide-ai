using System.Text.Json;
using AutoMapper;
using DriverService.Common;
using DriverService.Features.GetDrivers;
using DriverService.Messaging;
using DriverService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.DeactivateDriver;

public static class DeactivateDriverEndpoint
{
    public static void MapDeactivateDriver(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/drivers/{id:guid}/deactivate", Handle)
            .RequireAuthorization("SchoolAdmin");

    private static async Task<IResult> Handle(
        Guid id,
        ICurrentUser currentUser,
        DriverDbContext db,
        IMapper mapper,
        CancellationToken ct
    )
    {
        if (currentUser.SchoolId is not Guid schoolId)
            throw new ForbiddenException("No school context on this account.");

        var driver = await db.Drivers.FirstOrDefaultAsync(
            d => d.Id == id && d.SchoolId == schoolId,
            ct
        );

        if (driver is null)
            throw new NotFoundException(ResponseMessages.DriverNotFound);

        // Idempotent: deactivating an already-inactive driver succeeds quietly
        // rather than erroring on a double click, and publishes nothing — a
        // second event would make consumers reprocess a change that never
        // happened.
        if (driver.Deactivate())
        {
            var evt = new DriverStatusChangedEvent(
                driver.Id,
                driver.SchoolId,
                driver.Status.ToString(),
                DateTime.UtcNow
            );

            db.OutboxMessages.Add(
                new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = MessagingConstants.DriverDeactivatedKey,
                    Payload = JsonSerializer.Serialize(evt),
                    OccurredAt = DateTime.UtcNow,
                }
            );

            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(
            ApiResponse<DriverListItem>.Ok(
                mapper.Map<DriverListItem>(driver),
                ResponseMessages.DriverDeactivated
            )
        );
    }
}
