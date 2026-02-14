using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Parsers;

public class TechnicianDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string? HomeAddress { get; set; }
    public Coordinates? HomeLocation { get; set; }
    public Coordinates? OfficeLocation { get; set; }

    public string? StartsFrom { get; set; }
    public string? FinishesAt { get; set; }

    public string? MondayStart { get; set; }
    public string? MondayEnd { get; set; }
    public string? TuesdayStart { get; set; }
    public string? TuesdayEnd { get; set; }
    public string? WednesdayStart { get; set; }
    public string? WednesdayEnd { get; set; }
    public string? ThursdayStart { get; set; }
    public string? ThursdayEnd { get; set; }
    public string? FridayStart { get; set; }
    public string? FridayEnd { get; set; }
    public string? SaturdayStart { get; set; }
    public string? SaturdayEnd { get; set; }
    public string? SundayStart { get; set; }
    public string? SundayEnd { get; set; }

    public int? MinBreakMinutes { get; set; }
    public string? BreakWindowStart { get; set; }
    public string? BreakWindowEnd { get; set; }

    public int? MaxHoursPerDay { get; set; }
    public int? MaxHoursPerWeek { get; set; }

    public string? ServiceSkills { get; set; }

    public bool? CanDoPhysicallyDemanding { get; set; }
    public bool? IsSkilledInLivingWalls { get; set; }
    public bool? IsComfortableWithHeights { get; set; }
    public bool? HasLiftCertification { get; set; }
    public bool? HasPesticideCertification { get; set; }
    public bool? IsCitizen { get; set; }
    public bool? HasVehicle { get; set; }
}