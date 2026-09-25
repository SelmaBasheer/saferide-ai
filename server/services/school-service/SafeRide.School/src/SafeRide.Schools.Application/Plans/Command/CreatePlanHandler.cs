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
        var invalid = Validate(command);

        if (invalid is not null)
            return Result.Failure<Guid>(invalid);

        if (await plans.NameExistsAsync(command.Name.Trim(), null, ct))
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

    public async Task<Result> UpdateAsync(Guid id, CreatePlanCommand command, CancellationToken ct)
    {
        var invalid = Validate(command);

        if (invalid is not null)
            return Result.Failure(invalid);

        var plan = await plans.GetByIdAsync(id, ct);

        if (plan is null)
            return Result.Failure(PlanErrors.NotFound);

        // Excluding itself, so keeping the same name is not a conflict.
        if (await plans.NameExistsAsync(command.Name.Trim(), id, ct))
            return Result.Failure(PlanErrors.NameTaken);

        plan.Update(
            command.Name,
            command.Description,
            command.PriceInPaise,
            command.BusLimit,
            command.DurationMonths
        );

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
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

    /// Returns the first thing wrong, or null when the command is fine.
    private static Error? Validate(CreatePlanCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return PlanErrors.NameRequired;

        if (command.PriceInPaise < 0)
            return PlanErrors.PriceInvalid;

        if (command.DurationMonths is < 1 or > MaxDurationMonths)
            return PlanErrors.DurationInvalid;

        if (command.BusLimit is < 1)
            return PlanErrors.BusLimitInvalid;

        return null;
    }
}
