namespace RouteOptimizer.Core.Models;

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
