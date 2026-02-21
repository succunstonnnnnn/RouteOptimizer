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
            site.Availability = ParseAvailability(site);

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

    public static ServiceSiteAvailability ParseAvailability(ServiceSite site)
    {
        var availability = new ServiceSiteAvailability();

        var dayMappings = new (DayOfWeek Day, string? Start, string? End)[]
        {
            (DayOfWeek.Monday, site.MondayStart, site.MondayEnd),
            (DayOfWeek.Tuesday, site.TuesdayStart, site.TuesdayEnd),
            (DayOfWeek.Wednesday, site.WednesdayStart, site.WednesdayEnd),
            (DayOfWeek.Thursday, site.ThursdayStart, site.ThursdayEnd),
            (DayOfWeek.Friday, site.FridayStart, site.FridayEnd),
            (DayOfWeek.Saturday, site.SaturdayStart, site.SaturdayEnd),
            (DayOfWeek.Sunday, site.SundayStart, site.SundayEnd),
        };

        foreach (var (day, start, end) in dayMappings)
        {
            var startTime = ParseTimeSpan(start);
            var endTime = ParseTimeSpan(end);

            if (startTime.HasValue && endTime.HasValue)
            {
                availability.TimeWindows.Add(new TimeWindow
                {
                    DayOfWeek = day,
                    StartTime = startTime.Value,
                    EndTime = endTime.Value
                });
            }
        }

        // Fall back to Mon-Fri 8:00-17:00 if no availability fields were set
        if (availability.TimeWindows.Count == 0)
        {
            var defaultDays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };

            foreach (var day in defaultDays)
            {
                availability.TimeWindows.Add(new TimeWindow
                {
                    DayOfWeek = day,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0)
                });
            }
        }

        return availability;
    }

    private static TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeSpan.TryParse(value, out var result) ? result : null;
    }

    public static SkillsRequired InferSkillsRequired(ServiceSite site, Service service)
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

        var f = frequency.Trim().ToLowerInvariant();

        // Exact matches first
        var result = f switch
        {
            "1 week" or "weekly" or "1" => (VisitFrequency?)VisitFrequency.Weekly,
            "2 weeks" or "biweekly" or "bi-weekly" or "2" => VisitFrequency.BiWeekly,
            "3 weeks" or "3" => VisitFrequency.ThreeWeekly,
            "4 weeks" or "monthly" or "4" => VisitFrequency.FourWeeks,
            _ => null
        };
        if (result.HasValue) return result.Value;

        // Pattern-based matching for formats like "1x a week", "1x in 14 days", "2x a month"
        if (f.Contains("week") && (f.Contains("1x") || f.Contains("once")))
            return VisitFrequency.Weekly;
        if (f.Contains("week") && (f.Contains("2x") || f.Contains("twice")))
            return VisitFrequency.Weekly; 
        if (f.Contains("7 day"))
            return VisitFrequency.Weekly;
        if (f.Contains("14 day") || (f.Contains("2") && f.Contains("week")))
            return VisitFrequency.BiWeekly;
        if (f.Contains("21 day") || (f.Contains("3") && f.Contains("week")))
            return VisitFrequency.ThreeWeekly;
        if (f.Contains("28 day") || f.Contains("month") || (f.Contains("4") && f.Contains("week")))
            return VisitFrequency.FourWeeks;

        return VisitFrequency.BiWeekly;
    }
    public static int ParseVisitsPerWeek(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim().ToLowerInvariant();

       
        if (!s.Contains("week")) return 0;

        int i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;

        int start = i;
        while (i < s.Length && char.IsDigit(s[i])) i++;

        if (i == start) return 0;

        if (int.TryParse(s.Substring(start, i - start), out var n) && n > 0)
            return n;

        return 0;
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

    public static ServiceType ParseServiceType(string? jobType)
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
