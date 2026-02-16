namespace RouteOptimizer.Core.Models;

public class Route
{
    public string Id { get; set; } = string.Empty;
    public string TechnicianId { get; set; } = string.Empty;
    public List<RouteStop> Stops { get; set; } = new();

    public double TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalDrivingMinutes { get; set; }

    public bool IsValid { get; set; }
    public List<string> Violations { get; set; } = new();
}
