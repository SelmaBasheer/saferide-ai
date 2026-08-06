using System.Text.Json;
using AutoMapper;
using DriverService.Common;
using DriverService.Domain;
using DriverService.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.CreateDriver;

public static class CreateDriverEndpoint
{
    public static void MapCreateDriver(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/drivers", Handle).RequireAuthorization("SchoolAdmin");

    private static async Task<IResult> Handle(
        CreateDriverRequest request,
        ICurrentUser currentUser,
        DriverDbContext db,
        IValidator<CreateDriverRequest> validator,
        IMapper mapper,
        CancellationToken ct
    )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        if (currentUser.SchoolId is not Guid schoolId)
            throw new ForbiddenException("No school context on this account.");

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Drivers.AnyAsync(d => d.SchoolId == schoolId && d.Email == email, ct);
        if (exists)
            throw new ConflictException(ResponseMessages.DriverEmailExists);

        var driver = Driver.Create(
            schoolId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.LicenseNumber,
            request.LicenseExpiryDate
        );

        var evt = new DriverCreatedEvent(
            driver.Id,
            driver.SchoolId,
            driver.FirstName,
            driver.LastName,
            driver.Email,
            driver.Phone,
            DateTime.UtcNow
        );

        db.Drivers.Add(driver);
        db.OutboxMessages.Add(
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "driver-created",
                Payload = JsonSerializer.Serialize(evt),
                OccurredAt = DateTime.UtcNow,
            }
        );

        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/drivers/{driver.Id}",
            ApiResponse<CreateDriverResponse>.Ok(
                mapper.Map<CreateDriverResponse>(driver), // ← replaces CreateDriverResponse.From(driver)
                ResponseMessages.DriverCreated
            )
        );
    }
}
