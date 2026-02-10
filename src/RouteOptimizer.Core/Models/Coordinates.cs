namespace RouteOptimizer.Core.Models;

public class Coordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Location
{
    public string Id { get; set; } = string.Empty;
    public Coordinates Coordinates { get; set; } = new();
    public LocationType Type { get; set; }
    public string? TechnicianId { get; set; }
}

public class DistanceMatrix
{
    public List<Location> Locations { get; set; } = new();
    public double[,] Distances { get; set; } = new double[0, 0];

    private Dictionary<string, int>? _index;

    private void BuildIndex()
    {
        _index = Locations
            .Select((l, i) => new { l.Id, i })
            .ToDictionary(x => x.Id, x => x.i);
    }

    public double GetDistance(string fromId, string toId)
    {
        _index ??= BuildAndReturn();
        return Distances[_index[fromId], _index[toId]];
    }

    private Dictionary<string, int> BuildAndReturn()
    {
        BuildIndex();
        return _index!;
    }
}
