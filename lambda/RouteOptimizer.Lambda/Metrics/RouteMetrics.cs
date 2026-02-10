using RouteOptimizer.Lambda.Models;

namespace RouteOptimizer.Lambda.Metrics;

public class RouteMetrics
{
    public int TotalRoutes(RoutingResponse response)
        => response.Routes.Count;
    public int MaxVisitsPerTech(RoutingResponse response)
    => response.Routes.Max(r => r.VisitIds.Count);

    public int MinVisitsPerTech(RoutingResponse response)
        => response.Routes.Min(r => r.VisitIds.Count);

    public int TotalVisits(RoutingResponse response)
        => response.Routes.Sum(r => r.VisitIds.Count);
}
