using Xunit;
using RouteOptimizer.Lambda.Models;
using RouteOptimizer.Lambda.Validation;

namespace RouteOptimizer.Integration.Tests;

public class RouteValidatorTests
{
    [Fact]
    public void Should_Fail_When_No_Routes()
    {
        var response = new RoutingResponse();

        var validator = new RouteValidator();

        Assert.False(validator.HasRoutes(response));
    }

    [Fact]
    public void Should_Fail_On_Duplicate_Visits()
    {
        var response = new RoutingResponse
        {
            Routes =
            [
                new Route { TechnicianId = 1, VisitIds = [101] },
                new Route { TechnicianId = 2, VisitIds = [101] }
            ]
        };

        var validator = new RouteValidator();

        Assert.False(validator.NoDuplicateVisits(response));
    }
}
