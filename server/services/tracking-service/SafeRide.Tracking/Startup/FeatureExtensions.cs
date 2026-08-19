using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Boarding.MarkBoarding;
using SafeRide.Tracking.Features.Gps.IngestPosition;
using SafeRide.Tracking.Features.Trips.EndTrip;
using SafeRide.Tracking.Features.Trips.GetActiveTrips;
using SafeRide.Tracking.Features.Trips.GetTrip;
using SafeRide.Tracking.Features.Trips.ListTrips;
using SafeRide.Tracking.Features.Trips.StartTrip;

namespace SafeRide.Tracking.Startup;

public static class FeatureExtensions
{
    public static IServiceCollection AddFeatures(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<StartTripHandler>();
        services.AddScoped<EndTripHandler>();
        services.AddScoped<GetTripHandler>();
        services.AddScoped<ListTripsHandler>();
        services.AddScoped<GetActiveTripsHandler>();
        services.AddScoped<IngestPositionHandler>();
        services.AddScoped<MarkBoardingHandler>();

        services.Configure<TrackingOptions>(configuration.GetSection("Tracking"));

        return services;
    }
}
