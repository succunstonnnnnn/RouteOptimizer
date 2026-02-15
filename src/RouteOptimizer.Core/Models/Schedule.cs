namespace RouteOptimizer.Core.Models;

public class Schedule
{
    public List<Route> Routes { get; set; } = new();
    public int PlanningHorizonWeeks { get; set; }
    public DateTimeOffset StartDate { get; set; }

    public double TotalDistanceKm => Routes.Sum(r => r.TotalDistanceKm);
    public int TotalDrivingMinutes => Routes.Sum(r => r.TotalDrivingMinutes);

    public List<string> UnassignedVisitIds { get; set; } = new();
}
