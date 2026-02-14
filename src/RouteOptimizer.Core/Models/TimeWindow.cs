namespace RouteOptimizer.Core.Models;

public class TimeWindow
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsValidAt(DateTimeOffset visitDateTime)
    {
        if (visitDateTime.DayOfWeek != DayOfWeek)
            return false;

        var t = visitDateTime.TimeOfDay;

        if (StartTime <= EndTime)
            return t >= StartTime && t <= EndTime;

        return t >= StartTime || t <= EndTime;
    }

    public bool IsEmpty()
    {
        return StartTime == TimeSpan.Zero && EndTime == TimeSpan.Zero;
    }
}

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

public class BreakRequirement
{
    public int MinBreakMinutes { get; set; }
    public TimeSpan BreakWindowStart { get; set; }
    public TimeSpan BreakWindowEnd { get; set; }

    public bool IsBreakTimeValid(TimeSpan breakTime)
    {
        return breakTime >= BreakWindowStart && breakTime <= BreakWindowEnd;
    }
}
