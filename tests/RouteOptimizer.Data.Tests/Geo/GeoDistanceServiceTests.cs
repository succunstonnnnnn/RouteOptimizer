using RouteOptimizer.Core.Geo;

namespace RouteOptimizer.Data.Tests.Geo;

public class GeoDistanceServiceTests
{
    // New York: 40.7128, -74.0060
    // Los Angeles: 34.0522, -118.2437
    // Known distance: ~3,944 km

    [Fact]
    public void CalculateDistance_NewYorkToLosAngeles_AccurateInKm()
    {
        var distance = GeoDistanceService.CalculateDistance(
            40.7128, -74.0060,
            34.0522, -118.2437,
            DistanceUnit.Kilometers);

        Assert.InRange(distance, 3930, 3960);
    }

    [Fact]
    public void CalculateDistance_NewYorkToLosAngeles_AccurateInMiles()
    {
        var distance = GeoDistanceService.CalculateDistance(
            40.7128, -74.0060,
            34.0522, -118.2437,
            DistanceUnit.Miles);

        Assert.InRange(distance, 2440, 2460);
    }

    [Fact]
    public void CalculateDistance_SamePoint_ReturnsZero()
    {
        var distance = GeoDistanceService.CalculateDistance(
            40.7128, -74.0060,
            40.7128, -74.0060,
            DistanceUnit.Kilometers);

        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void CalculateDistance_IsSymmetric()
    {
        var d1 = GeoDistanceService.CalculateDistance(
            40.7128, -74.0060, 34.0522, -118.2437, DistanceUnit.Kilometers);
        var d2 = GeoDistanceService.CalculateDistance(
            34.0522, -118.2437, 40.7128, -74.0060, DistanceUnit.Kilometers);

        Assert.Equal(d1, d2, precision: 10);
    }

    [Fact]
    public void CalculateDistance_ShortDistance_Accurate()
    {
        // Two points ~1.11 km apart (0.01 degree latitude at same longitude)
        var distance = GeoDistanceService.CalculateDistance(
            40.7489, -73.9680,
            40.7589, -73.9680,
            DistanceUnit.Kilometers);

        Assert.InRange(distance, 1.0, 1.2);
    }

    [Fact]
    public void IsPointInGeofence_InsideRadius_ReturnsTrue()
    {
        // Two points ~1.1 km apart, radius 2 km
        var result = GeoDistanceService.IsPointInGeofence(
            40.7489, -73.9680,
            40.7589, -73.9680,
            2.0, "metric",
            out var distance);

        Assert.True(result);
        Assert.InRange(distance, 1.0, 1.2);
    }

    [Fact]
    public void IsPointInGeofence_OutsideRadius_ReturnsFalse()
    {
        // Two points ~1.1 km apart, radius 0.5 km
        var result = GeoDistanceService.IsPointInGeofence(
            40.7489, -73.9680,
            40.7589, -73.9680,
            0.5, "metric",
            out var distance);

        Assert.False(result);
        Assert.InRange(distance, 1.0, 1.2);
    }

    [Fact]
    public void IsPointInGeofence_ExactlyOnRadius_ReturnsTrue()
    {
        var dist = GeoDistanceService.CalculateDistance(
            40.7489, -73.9680, 40.7589, -73.9680, DistanceUnit.Kilometers);

        var result = GeoDistanceService.IsPointInGeofence(
            40.7489, -73.9680,
            40.7589, -73.9680,
            dist, "metric",
            out _);

        Assert.True(result);
    }

    [Fact]
    public void ParseDistanceUnit_Metric_ReturnsKilometers()
    {
        Assert.Equal(DistanceUnit.Kilometers, GeoDistanceService.ParseDistanceUnit("metric"));
    }

    [Fact]
    public void ParseDistanceUnit_Imperial_ReturnsMiles()
    {
        Assert.Equal(DistanceUnit.Miles, GeoDistanceService.ParseDistanceUnit("imperial"));
    }

    [Fact]
    public void ParseDistanceUnit_Null_DefaultsToMiles()
    {
        Assert.Equal(DistanceUnit.Miles, GeoDistanceService.ParseDistanceUnit(null));
    }

    [Fact]
    public void ParseDistanceUnit_Unknown_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            GeoDistanceService.ParseDistanceUnit("unknown"));
    }
}
