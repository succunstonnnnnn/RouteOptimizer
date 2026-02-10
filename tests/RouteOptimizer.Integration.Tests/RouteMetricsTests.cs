using Xunit;
using RouteOptimizer.Lambda.Models;
using RouteOptimizer.Lambda.Metrics;

namespace RouteOptimizer.Integration.Tests;

public class RouteMetricsTests
{
    [Fact]
    public void Should_Calculate_Total_Routes_And_Visits()
    {
        var response = new RoutingResponse
        {
            Routes =
            [
                new Route { TechnicianId = 1, VisitIds = [1,2,3] },
                new Route { TechnicianId = 2, VisitIds = [4] }
            ]
        };

        var metrics = new RouteMetrics();

        Assert.Equal(2, metrics.TotalRoutes(response));
        Assert.Equal(4, metrics.TotalVisits(response));
    }

    [Fact]
    public void Should_Calculate_Max_Min_Visits_Per_Tech()
    {
        var response = new RoutingResponse
        {
            Routes =
            [
                new Route { TechnicianId = 1, VisitIds = [1,2,3] },
                new Route { TechnicianId = 2, VisitIds = [4] }
            ]
        };

        var metrics = new RouteMetrics();

        Assert.Equal(3, metrics.MaxVisitsPerTech(response));
        Assert.Equal(1, metrics.MinVisitsPerTech(response));
    }
}
