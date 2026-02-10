using RouteOptimizer.Lambda.Models;

namespace RouteOptimizer.Lambda.Validation;

public class RouteValidator
{
    public bool HasRoutes(RoutingResponse response)
        => response.Routes != null && response.Routes.Count > 0;

    public bool NoEmptyRoutes(RoutingResponse response)
        => response.Routes.All(r => r.VisitIds.Any());

    public bool NoDuplicateVisits(RoutingResponse response)
    {
        var all = response.Routes.SelectMany(r => r.VisitIds);
        return all.Count() == all.Distinct().Count();
    }
}
