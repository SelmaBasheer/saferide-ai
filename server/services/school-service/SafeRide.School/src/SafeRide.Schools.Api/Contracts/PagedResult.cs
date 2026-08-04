namespace SafeRide.Schools.Api.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
