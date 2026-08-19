namespace SafeRide.Tracking.Infrastructure;

public sealed class ForwardAuthHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
