using AutoMapper;
using DriverService.Common;
using DriverService.Features.GetDrivers;
using DriverService.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Features.UpdateDriver;

public static class UpdateDriverEndpoint
{
    public static void MapUpdateDriver(this IEndpointRouteBuilder app) =>
        app.MapPut("/api/drivers/{id:guid}", Handle).RequireAuthorization("SchoolAdmin");

    private static async Task<IResult> Handle(
        Guid id,
        UpdateDriverRequest request,
        ICurrentUser currentUser,
        DriverDbContext db,
        IValidator<UpdateDriverRequest> validator,
        IMapper mapper,
        CancellationToken ct
    )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        if (currentUser.SchoolId is not Guid schoolId)
            throw new ForbiddenException("No school context on this account.");

        // School id is part of the lookup, not a check afterwards — a driver
        // from another school simply does not exist as far as this caller is
        // concerned, and 404 reveals less than 403.
        var driver = await db.Drivers.FirstOrDefaultAsync(
            d => d.Id == id && d.SchoolId == schoolId,
            ct
        );

        if (driver is null)
            throw new NotFoundException(ResponseMessages.DriverNotFound);

        driver.UpdateDetails(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.LicenseNumber,
            request.LicenseExpiryDate
        );

        await db.SaveChangesAsync(ct);

        return Results.Ok(
            ApiResponse<DriverListItem>.Ok(
                mapper.Map<DriverListItem>(driver),
                ResponseMessages.DriverUpdated
            )
        );
    }
}
