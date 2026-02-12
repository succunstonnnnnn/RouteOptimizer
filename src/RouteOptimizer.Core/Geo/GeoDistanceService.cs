namespace RouteOptimizer.Core.Geo;

public enum DistanceUnit
{
    Kilometers,
    Miles
}

public class GeoDistanceService
{
    private const double EarthRadiusKm = 6371.0;
    private const double EarthRadiusMi = 3958.8;

    public static bool IsPointInGeofence(double lat1, double lon1, double lat2, double lon2, double radius, string? unit, out double distance)
    {
        var distanceUnit = ParseDistanceUnit(unit);
        distance = CalculateDistance(lat1, lon1, lat2, lon2, distanceUnit);
        return distance <= radius;
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2, DistanceUnit unit)
    {
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double earthRadius = unit == DistanceUnit.Kilometers ? EarthRadiusKm : EarthRadiusMi;

        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    public static DistanceUnit ParseDistanceUnit(string? unit)
    {
        if (unit == null) return DistanceUnit.Miles;

        return unit.ToLower() switch
        {
            "metric" => DistanceUnit.Kilometers,
            "imperial" => DistanceUnit.Miles,
            _ => throw new ArgumentException($"Unknown distance unit: {unit}")
        };
    }
}
