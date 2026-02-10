namespace RouteOptimizer.Lambda.Models;

public class RoutingResponse
{
    public List<Route> Routes { get; set; } = new();
}

public class Route
{
    public int TechnicianId { get; set; }
    public List<int> VisitIds { get; set; } = new();
}
