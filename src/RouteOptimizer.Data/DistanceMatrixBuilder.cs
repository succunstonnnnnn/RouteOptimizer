using RouteOptimizer.Core.Geo;
using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data;

public class DistanceMatrixBuilder
{
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

        // 3. Build distance matrix using GeoDistanceService (parallelized)
        int n = locations.Count;
        var distances = new double[n, n];

        Parallel.For(0, n, i =>
        {
            for (int j = i + 1; j < n; j++)
            {
                var km = GeoDistanceService.CalculateDistance(
                    locations[i].Coordinates.Latitude,
                    locations[i].Coordinates.Longitude,
                    locations[j].Coordinates.Latitude,
                    locations[j].Coordinates.Longitude,
                    DistanceUnit.Kilometers);

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
}
