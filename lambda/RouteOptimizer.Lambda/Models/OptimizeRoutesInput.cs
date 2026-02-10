namespace RouteOptimizer.Lambda.Models;

public class OptimizeRoutesInput
{
    public string TenantId { get; set; } = "";
    public RoutingRequest Request { get; set; } = new();
}
