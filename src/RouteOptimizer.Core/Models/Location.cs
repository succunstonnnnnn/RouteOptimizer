namespace RouteOptimizer.Core.Models;

public class Location
{
    public string Id { get; set; } = string.Empty;
    public Coordinates Coordinates { get; set; } = new();
    public LocationType Type { get; set; }
    public string? TechnicianId { get; set; }
}
