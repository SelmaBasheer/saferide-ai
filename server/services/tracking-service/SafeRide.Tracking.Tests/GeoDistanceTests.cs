using SafeRide.Tracking.Domain;

namespace SafeRide.Tracking.Tests;

public class GeoDistanceTests
{
    // A short north-south line near Kollam. 0.01 degrees of latitude is about
    // 1,112 m, which makes the expected numbers checkable by hand.
    private static readonly (double Lat, double Lon)[] Path = [(8.89, 76.60), (8.90, 76.60)];

    [Fact]
    public void Metres_BetweenTheSamePoint_IsZero()
    {
        Assert.Equal(0, GeoDistance.Metres(8.89, 76.60, 8.89, 76.60), 6);
    }

    [Fact]
    public void Metres_ForOneDegreeOfLatitude_IsAboutOneHundredAndElevenKilometres()
    {
        var metres = GeoDistance.Metres(8.0, 76.60, 9.0, 76.60);

        // A degree of latitude is ~111.2 km anywhere on Earth. If this fails, the
        // haversine has a units or radians bug.
        Assert.InRange(metres, 111_000, 111_400);
    }

    [Fact]
    public void Metres_IsSymmetric()
    {
        var there = GeoDistance.Metres(8.89, 76.60, 8.92, 76.63);
        var back = GeoDistance.Metres(8.92, 76.63, 8.89, 76.60);

        Assert.Equal(there, back, 6);
    }

    [Fact]
    public void MetresToPath_ForAPointOnTheLine_IsAboutZero()
    {
        var metres = GeoDistance.MetresToPath(8.895, 76.60, Path);

        Assert.InRange(metres, 0, 1);
    }

    [Fact]
    public void MetresToPath_ForAPointBesideTheLine_IsThePerpendicularDistance()
    {
        // 0.001 degrees of longitude at this latitude is about 110 m.
        var metres = GeoDistance.MetresToPath(8.895, 76.601, Path);

        Assert.InRange(metres, 105, 115);
    }

    [Fact]
    public void MetresToPath_ForAPointBeyondTheEnd_MeasuresToTheEndpoint()
    {
        // 0.01 degrees north of the last point. The segment must not be treated
        // as an infinite line, or this would come back as ~0.
        var metres = GeoDistance.MetresToPath(8.91, 76.60, Path);

        Assert.InRange(metres, 1_050, 1_150);
    }

    [Fact]
    public void MetresToPath_WithFewerThanTwoPoints_IsMaxValue()
    {
        // A route with no drawn path must never look like a deviation of zero.
        Assert.Equal(double.MaxValue, GeoDistance.MetresToPath(8.89, 76.60, [(8.89, 76.60)]));
    }
}
