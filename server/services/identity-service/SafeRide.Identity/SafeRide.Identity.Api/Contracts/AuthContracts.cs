namespace SafeRide.Identity.Api.Contracts;

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresIn);

public sealed record RegisterResponse(Guid UserId);
