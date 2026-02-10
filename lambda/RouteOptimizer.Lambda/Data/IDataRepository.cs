using RouteOptimizer.Lambda.Models;

namespace RouteOptimizer.Lambda.Data;

public interface IDataRepository
{
    RoutingRequest LoadRequest(string tenantId);
}
