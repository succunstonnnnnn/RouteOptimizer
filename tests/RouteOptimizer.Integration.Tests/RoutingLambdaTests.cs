using Xunit;
using RouteOptimizer.Lambda;
using RouteOptimizer.Lambda.Models;
using RouteOptimizer.Lambda.Validation;
using RouteOptimizer.Lambda.Metrics;

namespace RouteOptimizer.Integration.Tests;

public class RoutingPipelineTests
{
    [Fact]
    public void Full_Pipeline_Should_Be_Valid()
    {
        var handler = new RoutingHandler();
        var validator = new RouteValidator();
        var metrics = new RouteMetrics();

        var request = new RoutingRequest
        {
            Technicians = 2,
            Visits = 5
        };

        var response = handler.Handle(request);

        Assert.True(validator.HasRoutes(response));
        Assert.True(metrics.TotalRoutes(response) > 0);
    }
}
