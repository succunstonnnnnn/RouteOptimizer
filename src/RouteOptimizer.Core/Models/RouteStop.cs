namespace RouteOptimizer.Core.Models;

public class RouteStop
{
    public int Sequence { get; set; }
    public string VisitInstanceId { get; set; } = string.Empty;
    public string ServiceSiteId { get; set; } = string.Empty;

    public DateTimeOffset ArrivalTime { get; set; }
    public DateTimeOffset DepartureTime { get; set; }

    public double DistanceFromPreviousKm { get; set; }
    public int DrivingTimeMinutes { get; set; }

    public bool IsWalkingFromPrevious { get; set; }
}
