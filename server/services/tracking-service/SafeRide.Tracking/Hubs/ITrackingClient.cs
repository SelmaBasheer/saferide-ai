using SafeRide.Tracking.Hubs.Contracts;

namespace SafeRide.Tracking.Hubs;

public interface ITrackingClient
{
    Task PositionUpdated(PositionUpdate update);

    Task TripStarted(TripLifecycleNotification notification);

    Task TripEnded(TripLifecycleNotification notification);

    Task StopReached(StopReachedNotification notification);

    Task ApproachingStop(ApproachingStopNotification notification);

    Task StudentBoarded(StudentBoardedNotification notification);

    Task RouteDeviation(RouteDeviationNotification notification);
}
