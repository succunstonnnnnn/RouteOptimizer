namespace RouteOptimizer.Core.Models;

public class ServiceSiteAvailability
{
    public List<TimeWindow> TimeWindows { get; set; } = new();

    public bool IsAvailableAt(DateTimeOffset visitDateTime)
    {
        return TimeWindows.Any(tw => tw.IsValidAt(visitDateTime));
    }

    public bool IsAvailableOnDay(DayOfWeek day)
    {
        return TimeWindows.Any(tw => tw.DayOfWeek == day && !tw.IsEmpty());
    }

    public TimeWindow? GetWindowForDay(DayOfWeek day)
    {
        return TimeWindows.FirstOrDefault(tw => tw.DayOfWeek == day);
    }
}
