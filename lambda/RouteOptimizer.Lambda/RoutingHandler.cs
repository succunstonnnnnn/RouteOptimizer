using RouteOptimizer.Lambda.Data;
using RouteOptimizer.Lambda.Models;

namespace RouteOptimizer.Lambda;

public class RoutingHandler
{
    private readonly IDataRepository? _repo;

    
    public RoutingHandler()
    {
    }

    
    public RoutingHandler(IDataRepository repo)
    {
        _repo = repo;
    }

    
    public RoutingResponse Handle(RoutingRequest request)
    {
        // Fake логіка поки алгоритму нема
        return new RoutingResponse
        {
            Routes = new List<Route>
            {
                new Route
                {
                    TechnicianId = 1,
                    VisitIds = new List<int> { 101, 102 }
                }
            }
        };
    }

    
    public RoutingResponse Handle(string tenantId)
    {
        if (_repo == null)
            throw new InvalidOperationException(
                "Repo не передали в RoutingHandler. Використай: new RoutingHandler(repo).");

        var request = _repo.LoadRequest(tenantId);
        return Handle(request); // пере-використовуємо стару логіку
    }
}
