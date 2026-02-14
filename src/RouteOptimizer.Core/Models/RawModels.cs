namespace RouteOptimizer.Core.Models;

public class ServiceSite
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public Coordinates? Coordinates { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public bool? IsDeleted { get; set; }

    public List<Floor>? SiteFloors { get; set; }
    public List<string>? FileIds { get; set; }
    public List<string>? FileUrls { get; set; }
    public List<string>? FileNames { get; set; }
    public List<string>? ImageIds { get; set; }
    public List<string>? Images { get; set; }

    public string? Parking { get; set; }
    public string? Access { get; set; }
    public string? Water { get; set; }
    public string? DeliveryInstructions { get; set; }
    public string? Other { get; set; }

    public string? AccountManagerUserId { get; set; }
    public string? AccountManagerUserName { get; set; }

    public int? TotalPlants { get; set; }

    public List<ServiceSiteContact>? Contacts { get; set; }

    public string? IndustryType { get; set; }
    public string? Revenue { get; set; }
    public string? WateringType { get; set; }
    public string? Division { get; set; }

    public DateTimeOffset? LastUpdate { get; set; }

    public List<Service>? Services { get; set; }

    public DateTimeOffset? NextVisit { get; set; }

    public TransportType BestAccessedBy { get; set; }

    public bool RequiresPermit { get; set; }
    public PermitDifficulty PermitDifficulty { get; set; }

    public List<string>? TechsWithPermit { get; set; }

    public List<string>? MustBeServicedWithSiteIds { get; set; }

    // Per-day availability (flat fields for JSON/Excel compatibility)
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

    [System.Text.Json.Serialization.JsonIgnore]
    public ServiceSiteAvailability Availability { get; set; } = new();
}

public class Service
{
    public string Id { get; set; } = string.Empty;
    public string? JobType { get; set; }
    public string SiteId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
    public bool? IsDeleted { get; set; }

    public string? TechUserId { get; set; }
    public string? TechUserName { get; set; }

    public string? SupervisorUserId { get; set; }
    public string? SupervisorUserName { get; set; }

    public int? TotalDisplays { get; set; }
    public int? TotalReplacementRequests { get; set; }
    public int? TotalIssues { get; set; }
    public int? TotalAssignments { get; set; }
    public int? TotalPlants { get; set; }

    public string? AccountStatus { get; set; }
    public string? ContractType { get; set; }

    public DateTimeOffset? ContractStart { get; set; }
    public bool? ContractStartClear { get; set; }
    public DateTimeOffset? ContractEnd { get; set; }
    public bool? ContractEndClear { get; set; }

    public DateTimeOffset? DateOfInstall { get; set; }
    public bool? DateOfInstallClear { get; set; }

    public string? FrequencyOfVisits { get; set; }
    public bool? FrequencyOfVisitsClear { get; set; }

    public DateTimeOffset? LastUpdate { get; set; }
    public DateTimeOffset? NextVisit { get; set; }

    public string? Route { get; set; }
    public int EstimatedDurationMinutes { get; set; }

    public VisitFrequency VisitFrequency { get; set; }

    public SkillsRequired? SkillsRequired { get; set; }

    public bool PhysicallyDemanding { get; set; }
    public bool RequiresLivingWalls { get; set; }
    public bool RequiresHeightWork { get; set; }
    public bool RequiresLift { get; set; }
    public bool RequiresPesticides { get; set; }
    public bool RequiresCitizen { get; set; }

    public List<string>? AllowedTechnicianIds { get; set; }
    public List<string>? ForbiddenTechnicianIds { get; set; }

}

public class Floor
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Rooms { get; set; }
    public List<string>? Zones { get; set; }
}

public class ServiceSiteContact
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
}
