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

