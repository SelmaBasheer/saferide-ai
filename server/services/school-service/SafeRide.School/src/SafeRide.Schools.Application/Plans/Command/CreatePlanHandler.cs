using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Plans.Command;

public sealed class CreatePlanHandler(ISubscriptionPlanRepository plans, IUnitOfWork unitOfWork)
{
    private const int MaxDurationMonths = 36;

    public async Task<Result<Guid>> CreateAsync(CreatePlanCommand command, CancellationToken ct)
    {
        // User input is validated here and comes back as an error code. The
        // domain factory guards the same rules by throwing, because reaching
        // it with bad values would mean a bug rather than a bad request.
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result.Failure<Guid>(PlanErrors.NameRequired);

        if (command.PriceInPaise < 0)
            return Result.Failure<Guid>(PlanErrors.PriceInvalid);

        if (command.DurationMonths is < 1 or > MaxDurationMonths)
            return Result.Failure<Guid>(PlanErrors.DurationInvalid);

        if (command.BusLimit is < 1)
            return Result.Failure<Guid>(PlanErrors.BusLimitInvalid);

        if (await plans.NameExistsAsync(command.Name.Trim(), ct))
            return Result.Failure<Guid>(PlanErrors.NameTaken);

        var plan = SubscriptionPlan.Create(
            command.Name,
            command.Description,
            command.PriceInPaise,
            command.BusLimit,
            command.DurationMonths
        );

        await plans.AddAsync(plan, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(plan.Id);
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var plan = await plans.GetByIdAsync(id, ct);

        if (plan is null)
            return Result.Failure(PlanErrors.NotFound);

        plan.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
