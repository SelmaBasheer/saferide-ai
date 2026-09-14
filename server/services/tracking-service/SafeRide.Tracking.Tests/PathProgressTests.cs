using SafeRide.Tracking.Domain;

namespace SafeRide.Tracking.Tests;

public class PathProgressTests
{
    // Three points in a straight line north, each leg about 1,112 m.
    private static readonly (double Lat, double Lon)[] Path =
    [
        (8.89, 76.60),
        (8.90, 76.60),
        (8.91, 76.60),
    ];

    [Fact]
    public void Cumulative_StartsAtZero()
    {
        Assert.Equal(0, PathProgress.Cumulative(Path)[0]);
    }

    [Fact]
    public void Cumulative_NeverDecreases()
    {
        var distances = PathProgress.Cumulative(Path);

        for (var i = 1; i < distances.Length; i++)
        {
            Assert.True(distances[i] >= distances[i - 1], $"Distance went backwards at index {i}");
        }
    }

    [Fact]
    public void Cumulative_EndsAtTheTotalLength()
    {
        var distances = PathProgress.Cumulative(Path);

        // Two equal legs, so the end should be twice the first leg.
        Assert.Equal(distances[1] * 2, distances[2], 3);
    }

    [Fact]
    public void DistanceAlong_AtTheStart_IsAboutZero()
    {
        var distances = PathProgress.Cumulative(Path);
        var along = PathProgress.DistanceAlong(8.89, 76.60, Path, distances);

        Assert.InRange(along, 0, 1);
    }

    [Fact]
    public void DistanceAlong_HalfwayAlongTheFirstLeg_IsAboutHalfOfIt()
    {
        var distances = PathProgress.Cumulative(Path);
        var along = PathProgress.DistanceAlong(8.895, 76.60, Path, distances);

        Assert.InRange(along, 540, 570);
    }

    [Fact]
    public void DistanceAlong_ForAPointBesideTheLine_IgnoresTheSidewaysOffset()
    {
        var distances = PathProgress.Cumulative(Path);

        var onLine = PathProgress.DistanceAlong(8.895, 76.60, Path, distances);
        var besideLine = PathProgress.DistanceAlong(8.895, 76.601, Path, distances);

        // A bus 110 m off the centre line is still halfway along the route. This
        // is what lets an ETA survive a driver taking the other side of a
        // dual carriageway.
        Assert.InRange(Math.Abs(onLine - besideLine), 0, 5);
    }

    [Fact]
    public void DistanceAlong_BeyondTheEnd_StopsAtTheEnd()
    {
        var distances = PathProgress.Cumulative(Path);
        var total = distances[^1];

        var along = PathProgress.DistanceAlong(8.95, 76.60, Path, distances);

        // The projection clamps to the segment, so a bus that overshoots cannot
        // report a distance longer than the route itself — which would make the
        // ETA nonsense.
        Assert.InRange(along, total - 10, total + 10);
    }

    [Fact]
    public void DistanceAlong_WithFewerThanTwoPoints_IsZero()
    {
        Assert.Equal(0, PathProgress.DistanceAlong(8.89, 76.60, [(8.89, 76.60)], [0]));
    }
}
