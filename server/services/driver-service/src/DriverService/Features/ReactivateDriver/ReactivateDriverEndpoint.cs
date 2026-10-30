using System.Text.Json;
using AutoMapper;
using DriverService.Common;
using DriverService.Features.GetDrivers;
using DriverService.Messaging;
using DriverService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.ReactivateDriver;

public static class ReactivateDriverEndpoint
{
    public static void MapReactivateDriver(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/drivers/{id:guid}/reactivate", Handle)
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

        if (driver.Reactivate())
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
                    Type = MessagingConstants.DriverReactivatedKey,
                    Payload = JsonSerializer.Serialize(evt),
                    OccurredAt = DateTime.UtcNow,
                }
            );

            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(
            ApiResponse<DriverListItem>.Ok(
                mapper.Map<DriverListItem>(driver),
                ResponseMessages.DriverReactivated
            )
        );
    }
}
