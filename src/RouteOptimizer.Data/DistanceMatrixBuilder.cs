using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data;

public class DistanceMatrixBuilder
{
    private const double EarthRadiusKm = 6371.0;

    public DistanceMatrix Build(
        IReadOnlyList<VisitInstance> visits,
        IReadOnlyList<Technician> technicians)
    {
        var locations = new List<Location>();

        // 1. Technician start / end locations (fixed order)
        foreach (var tech in technicians.OrderBy(t => t.Id))
        {
            var start = tech.GetStartLocation();
            locations.Add(new Location
            {
                Id = $"tech_{tech.Id}_start",
                Coordinates = start,
                Type = LocationType.TechnicianHome,
                TechnicianId = tech.Id
            });

            var end = tech.GetEndLocation();
            if (tech.StartsFrom != tech.FinishesAt)
            {
                locations.Add(new Location
                {
                    Id = $"tech_{tech.Id}_end",
                    Coordinates = end,
                    Type = LocationType.TechnicianHome,
                    TechnicianId = tech.Id
                });
            }
        }

        // 2. Unique service sites (fixed order)
        var sites = visits
            .GroupBy(v => v.ServiceSiteId)
            .Select(g => g.First())
            .OrderBy(v => v.ServiceSiteId)
            .ToList();

        foreach (var v in sites)
        {
            locations.Add(new Location
            {
                Id = v.ServiceSiteId,
                Coordinates = new Coordinates
                {
                    Latitude = v.Latitude,
                    Longitude = v.Longitude
                },
                Type = LocationType.ServiceSite
            });
        }

        // 3. Pre-compute radian values for all locations
        int n = locations.Count;
        var latRad = new double[n];
        var lonRad = new double[n];
        for (int k = 0; k < n; k++)
        {
            latRad[k] = ToRad(locations[k].Coordinates.Latitude);
            lonRad[k] = ToRad(locations[k].Coordinates.Longitude);
        }

        // 4. Build distance matrix (parallelized for 100+ locations)
        var distances = new double[n, n];

        Parallel.For(0, n, i =>
        {
            for (int j = i + 1; j < n; j++)
            {
                var km = HaversineKmRad(latRad[i], lonRad[i], latRad[j], lonRad[j]);
                distances[i, j] = km;
                distances[j, i] = km;
            }
        });

        return new DistanceMatrix
        {
            Locations = locations,
            Distances = distances
        };
    }

    private static double HaversineKmRad(
        double lat1Rad, double lon1Rad,
        double lat2Rad, double lon2Rad)
    {
        var dLat = lat2Rad - lat1Rad;
        var dLon = lon2Rad - lon1Rad;

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRad(double deg)
        => deg * Math.PI / 180.0;
}
