namespace RouteOptimizer.Core.Models;

public class Technician
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Coordinates HomeLocation { get; set; } = new();
    public Coordinates? OfficeLocation { get; set; }

    public WorkLocation StartsFrom { get; set; } = WorkLocation.Home;
    public WorkLocation FinishesAt { get; set; } = WorkLocation.Home;

    public TechnicianSkills Skills { get; set; } = new();

    public List<DayOfWeek> WorkingDays { get; set; } = new();
    public Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> DailySchedule { get; set; } = new();

    public int MaxHoursPerDay { get; set; } = 8;
    public int MaxHoursPerWeek { get; set; } = 40;

    public BreakRequirement BreakRequirement { get; set; } = new();

    public bool HasVehicle { get; set; } = true;

    public bool CanWorkOn(DayOfWeek day)
    {
        return WorkingDays.Contains(day);
    }

    public bool CanWorkOn(DayOfWeek day, TimeSpan start, TimeSpan end)
    {
        if (!WorkingDays.Contains(day))
            return false;

        if (!DailySchedule.TryGetValue(day, out var schedule))
            return false;

        return start >= schedule.Start && end <= schedule.End;
    }

    public (TimeSpan Start, TimeSpan End)? GetScheduleForDay(DayOfWeek day)
    {
        if (DailySchedule.TryGetValue(day, out var schedule))
            return schedule;

        return null;
    }


    public Coordinates GetStartLocation()
    {
        return StartsFrom switch
        {
            WorkLocation.Home => HomeLocation,
            WorkLocation.Office => OfficeLocation ?? HomeLocation,
            WorkLocation.Either => HomeLocation,
            _ => HomeLocation
        };
    }

    public Coordinates GetEndLocation()
    {
        return FinishesAt switch
        {
            WorkLocation.Home => HomeLocation,
            WorkLocation.Office => OfficeLocation ?? HomeLocation,
            WorkLocation.Either => HomeLocation,
            _ => HomeLocation
        };
    }
}
