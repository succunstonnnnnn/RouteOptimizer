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
