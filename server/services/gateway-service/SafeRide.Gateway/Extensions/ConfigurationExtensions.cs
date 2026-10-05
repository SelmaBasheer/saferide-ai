using Azure.Identity;

namespace SafeRide.Gateway.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Layered over appsettings, so vault values win. With no vault configured
    /// the app falls back to environment variables, which keeps it runnable by
    /// someone without an Azure subscription.
    /// </summary>
    public static WebApplicationBuilder AddKeyVault(this WebApplicationBuilder builder)
    {
        var uri = builder.Configuration["KeyVault:Uri"];

        if (string.IsNullOrWhiteSpace(uri))
        {
            return builder;
        }

        try
        {
            builder.Configuration.AddAzureKeyVault(new Uri(uri), new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            // A developer without Azure access should still be able to run the
            // gateway with JwtSettings__Secret in the environment. A genuinely
            // missing secret is still caught when authentication is configured,
            // so this cannot hide a real misconfiguration.
            Console.Error.WriteLine(
                $"Key Vault unavailable, falling back to environment: {ex.Message}"
            );
        }

        return builder;
    }
}
