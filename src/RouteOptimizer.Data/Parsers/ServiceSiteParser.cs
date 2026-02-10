using System.Text.Json;
using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Parsers;

public class ServiceSiteParser
{
    public List<ServiceSite> ParseFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var sites = JsonSerializer.Deserialize<List<ServiceSite>>(jsonContent, options)
                    ?? new List<ServiceSite>();

        foreach (var site in sites)
        {
            if (site.Services == null) continue;
            foreach (var service in site.Services)
            {
                service.VisitFrequency = ParseFrequencyString(service.FrequencyOfVisits);
                if (service.EstimatedDurationMinutes <= 0)
                    service.EstimatedDurationMinutes = DefaultDurationMinutes(service.JobType);
            }
        }

        return sites;
    }

    public ServiceSiteAvailability ParseAvailability(ServiceSite site)
    {
        var availability = new ServiceSiteAvailability();

        // TODO: Parse from actual fields when they're added to ServiceSite
        // For now, default to business hours Mon-Fri 8:00-17:00
        var daysOfWeek = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        foreach (var day in daysOfWeek)
        {
            availability.TimeWindows.Add(new TimeWindow
            {
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });
        }

        return availability;
    }

    public SkillsRequired InferSkillsRequired(ServiceSite site, Service service)
    {
        return new SkillsRequired
        {
            ServiceType = ParseServiceType(service.JobType),
            MinimumSkillLevel = service.SkillsRequired?.MinimumSkillLevel ?? SkillLevel.Junior,
            IsPhysicallyDemanding = service.PhysicallyDemanding,
            RequiresLivingWalls = service.RequiresLivingWalls,
            RequiresHeightWork = service.RequiresHeightWork,
            RequiresLift = service.RequiresLift,
            RequiresPesticideCertification = service.RequiresPesticides,
            RequiresCitizenship = service.RequiresCitizen,
            PreferredTransport = site.BestAccessedBy
        };
    }

    public static VisitFrequency ParseFrequencyString(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            return VisitFrequency.BiWeekly;

        return frequency.Trim().ToLowerInvariant() switch
        {
            "1 week" or "weekly" or "1" => VisitFrequency.Weekly,
            "2 weeks" or "biweekly" or "bi-weekly" or "2" => VisitFrequency.BiWeekly,
            "3 weeks" or "3" => VisitFrequency.ThreeWeekly,
            "4 weeks" or "monthly" or "4" => VisitFrequency.FourWeeks,
            _ => VisitFrequency.BiWeekly
        };
    }

    private static int DefaultDurationMinutes(string? jobType)
    {
        return jobType?.ToLowerInvariant() switch
        {
            "exterior" => 60,
            "interior" => 45,
            "floral" => 30,
            _ => 45
        };
    }

    private static ServiceType ParseServiceType(string? jobType)
    {
        if (string.IsNullOrEmpty(jobType))
            return ServiceType.Interior;

        return jobType.ToLower() switch
        {
            "interior" => ServiceType.Interior,
            "exterior" => ServiceType.Exterior,
            "floral" => ServiceType.Floral,
            _ => ServiceType.Interior
        };
    }
}
