namespace SafeRide.Tracking.Domain;

public enum PositionSource
{
    Gps,
    Simulated, //Every stored point is tagged, so demo data stays distinguishable from real data forever.
}
