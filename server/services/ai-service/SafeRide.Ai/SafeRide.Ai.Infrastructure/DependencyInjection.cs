using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Infrastructure.Ai;
using SafeRide.Ai.Infrastructure.Messaging;
using SafeRide.Ai.Infrastructure.Persistence;

namespace SafeRide.Ai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AiDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAnomalyRepository, AnomalyRepository>();
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName)
        );
        services.AddHostedService<TrackingEventConsumer>();
        services.AddScoped<IAnomalyClassifier, TemplateAnomalyClassifier>();

        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        services.AddSingleton<TemplateAnomalyClassifier>();

        var geminiKey = configuration[$"{GeminiSettings.SectionName}:ApiKey"];

        if (string.IsNullOrWhiteSpace(geminiKey))
        {
            // No key configured: alerts still appear, just without interpretation.
            Console.WriteLine("[AI] No Gemini key configured — using template classifier.");
            services.AddScoped<IAnomalyClassifier>(sp =>
                sp.GetRequiredService<TemplateAnomalyClassifier>()
            );
        }
        else
        {
            Console.WriteLine("[AI] Gemini key found — using model classifier.");
            services.AddHttpClient<IAnomalyClassifier, GeminiAnomalyClassifier>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("x-goog-api-key", geminiKey);
            });
        }

        return services;
    }
}
