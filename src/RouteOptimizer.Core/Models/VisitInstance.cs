using System.Globalization;

namespace RouteOptimizer.Core.Models;


public class VisitInstance
{
    public string Id { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceSiteId { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int WeekNumber =>
        ISOWeek.GetWeekOfYear(ScheduledDate.Date);
    public DateTimeOffset ScheduledDate { get; set; }
    public int DurationMinutes { get; set; }


    public List<TimeWindow> TimeWindows { get; set; } = new();

    public SkillsRequired SkillsRequired { get; set; } = new();
    public List<string>? AllowedTechnicianIds { get; set; }
    public List<string>? ForbiddenTechnicianIds { get; set; }
    public List<string>? SecurityClearanceTechnicianIds { get; set; }

    public string SiteName { get; set; } = string.Empty;
    public string SiteAddress { get; set; } = string.Empty;

    public string? AssignedTechnicianId { get; set; }
    public bool IsAssigned { get; set; }
    public string? RouteId { get; set; }
}

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

public class Schedule
{
    public List<Route> Routes { get; set; } = new();
    public int PlanningHorizonWeeks { get; set; }
    public DateTimeOffset StartDate { get; set; }

    public double TotalDistanceKm => Routes.Sum(r => r.TotalDistanceKm);
    public int TotalDrivingMinutes => Routes.Sum(r => r.TotalDrivingMinutes);

    public List<string> UnassignedVisitIds { get; set; } = new();
}
