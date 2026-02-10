namespace RouteOptimizer.Lambda.Models;

public class OptimizeRoutesPayload
{
    public string TenantId { get; set; } = "";
    public RoutingResponse Result { get; set; } = new();
}
