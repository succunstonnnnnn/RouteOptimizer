using System.Text.Json;
using RouteOptimizer.Lambda.Models;

namespace RouteOptimizer.Lambda.Data;

public class FileDataRepository : IDataRepository
{
    private readonly string _basePath;

    public FileDataRepository(string basePath)
    {
        _basePath = basePath;
    }

    public RoutingRequest LoadRequest(string tenantId)
    {
        // поки просто беремо один файл, tenantId потім знадобиться для DynamoDB
        var path = Path.Combine(_basePath, "samples", "routing", "routing-request.json");
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<RoutingRequest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;
    }
}
