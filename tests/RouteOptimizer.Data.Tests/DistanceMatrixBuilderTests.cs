using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Tests;

public class DistanceMatrixBuilderTests
{
    private readonly DistanceMatrixBuilder _builder = new();

    [Fact]
    public void Build_EmptyInputs_ReturnsEmptyMatrix()
    {
        var result = _builder.Build(
            Array.Empty<VisitInstance>().ToList(),
            Array.Empty<Technician>().ToList()
        );

        Assert.Empty(result.Locations);
        Assert.Equal(0, result.Distances.GetLength(0));
    }

    [Fact]
    public void Build_SingleTechSingleSite_CorrectDimensions()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 }
        };
        var techs = new List<Technician>
        {
            new()
            {
                Id = "tech-001",
                HomeLocation = new Coordinates { Latitude = 40.7589, Longitude = -73.9851 },
                StartsFrom = WorkLocation.Home,
                FinishesAt = WorkLocation.Home
            }
        };

        var result = _builder.Build(visits, techs);

        // 1 tech location (start=end when same WorkLocation) + 1 site = 2
        Assert.Equal(2, result.Locations.Count);
        Assert.Equal(2, result.Distances.GetLength(0));
        Assert.Equal(2, result.Distances.GetLength(1));
    }

    [Fact]
    public void Build_TechWithDifferentStartEnd_AddsBothLocations()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 }
        };
        var techs = new List<Technician>
        {
            new()
            {
                Id = "tech-001",
                HomeLocation = new Coordinates { Latitude = 40.7589, Longitude = -73.9851 },
                OfficeLocation = new Coordinates { Latitude = 40.7300, Longitude = -73.9900 },
                StartsFrom = WorkLocation.Home,
                FinishesAt = WorkLocation.Office
            }
        };

        var result = _builder.Build(visits, techs);

        // 2 tech locations (start != end) + 1 site = 3
        Assert.Equal(3, result.Locations.Count);
    }

    [Fact]
    public void Build_KnownDistance_HaversineIsAccurate()
    {
        // Two points ~1.11 km apart (same longitude, 0.01 degree latitude difference)
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 },
            new() { ServiceSiteId = "site-002", Latitude = 40.7589, Longitude = -73.9680 }
        };

        var result = _builder.Build(visits, new List<Technician>());

        var dist = result.GetDistance("site-001", "site-002");
        Assert.InRange(dist, 1.0, 1.2);
    }

    [Fact]
    public void Build_DistanceIsSymmetric()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 },
            new() { ServiceSiteId = "site-002", Latitude = 40.7128, Longitude = -74.0060 }
        };

        var result = _builder.Build(visits, new List<Technician>());

        var d1 = result.GetDistance("site-001", "site-002");
        var d2 = result.GetDistance("site-002", "site-001");
        Assert.Equal(d1, d2, precision: 10);
    }

    [Fact]
    public void Build_DiagonalIsZero()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 }
        };

        var result = _builder.Build(visits, new List<Technician>());

        Assert.Equal(0.0, result.GetDistance("site-001", "site-001"));
    }

    [Fact]
    public void Build_DuplicateSitesDeduped()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 },
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 }
        };

        var result = _builder.Build(visits, new List<Technician>());

        Assert.Single(result.Locations);
    }

    [Fact]
    public void Build_TechLocationsOrderedById()
    {
        var visits = new List<VisitInstance>
        {
            new() { ServiceSiteId = "site-001", Latitude = 40.7489, Longitude = -73.9680 }
        };
        var techs = new List<Technician>
        {
            new()
            {
                Id = "tech-002",
                HomeLocation = new Coordinates { Latitude = 40.73, Longitude = -73.99 },
                StartsFrom = WorkLocation.Home, FinishesAt = WorkLocation.Home
            },
            new()
            {
                Id = "tech-001",
                HomeLocation = new Coordinates { Latitude = 40.76, Longitude = -73.99 },
                StartsFrom = WorkLocation.Home, FinishesAt = WorkLocation.Home
            }
        };

        var result = _builder.Build(visits, techs);

        Assert.Equal("tech_tech-001_start", result.Locations[0].Id);
        Assert.Equal("tech_tech-002_start", result.Locations[1].Id);
    }
}