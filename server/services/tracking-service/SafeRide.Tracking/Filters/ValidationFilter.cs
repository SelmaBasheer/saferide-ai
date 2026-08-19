using FluentValidation;

namespace SafeRide.Tracking.Common;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is not null)
        {
            var result = await validator.ValidateAsync(argument);

            if (!result.IsValid)
            {
                var first = result.Errors[0];
                throw AppException.Validation($"{first.PropertyName}: {first.ErrorMessage}");
            }
        }

        return await next(context);
    }
}
