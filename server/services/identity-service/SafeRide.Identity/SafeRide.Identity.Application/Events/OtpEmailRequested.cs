namespace SafeRide.Identity.Application.Events;

public sealed record OtpEmailRequested(
    Guid UserId,
    string Email,
    string FirstName,
    string Code,
    string Purpose,
    DateTime OccurredAtUtc
);
