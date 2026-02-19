using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Preprocessing;

public class VisitGenerator
{

    public List<VisitInstance> GenerateVisits(
        List<ServiceSite> sites,
        DateTimeOffset startDate,
        int planningHorizonWeeks)
    {
        var visits = new List<VisitInstance>();

        foreach (var site in sites)
        {
            if (site.Services == null) continue;

            foreach (var service in site.Services)
            {
                if (service.IsDeleted == true || site.IsDeleted == true) continue;

                var serviceVisits = GenerateVisitsForService(
                    site,
                    service,
                    startDate,
                    planningHorizonWeeks
                );
                visits.AddRange(serviceVisits);
            }
        }

        return visits;
    }

    private List<VisitInstance> GenerateVisitsForService(
        ServiceSite site,
        Service service,
        DateTimeOffset startDate,
        int planningHorizonWeeks)
    {
        var visits = new List<VisitInstance>();
        var intervalWeeks = (int)service.VisitFrequency;
        if (intervalWeeks <= 0)
            return visits;

        var totalVisits = (int)Math.Ceiling(
            planningHorizonWeeks / (double)intervalWeeks
        );


        var availability = site.Availability;
        var skillsRequired = ServiceSiteParser.InferSkillsRequired(site, service);

        for (int i = 0; i < totalVisits; i++)
        {
            var scheduledDate = startDate.AddDays(i * intervalWeeks * 7);

            // Find first available day in that week
            var availableDate = FindDistributedAvailableDate(scheduledDate, availability, site.Id, service.Id);

            if (availableDate == null) continue; // Skip if no availability

            var visit = new VisitInstance
            {
                Id = $"{site.Id}-{service.Id}-W{i + 1}",
                ServiceId = service.Id,
                ServiceSiteId = site.Id,
                Latitude = site.Coordinates?.Latitude ?? 0,
                Longitude = site.Coordinates?.Longitude ?? 0,
                ScheduledDate = availableDate.Value,
                DurationMinutes = service.EstimatedDurationMinutes,
                TimeWindows = availability.TimeWindows
                    .Where(tw => tw.DayOfWeek == availableDate.Value.DayOfWeek)
                    .ToList(),
                SkillsRequired = skillsRequired,
                AllowedTechnicianIds = service.AllowedTechnicianIds,
                ForbiddenTechnicianIds = service.ForbiddenTechnicianIds,
                SecurityClearanceTechnicianIds = site.TechsWithPermit,
                SiteName = site.Name ?? "Unknown",
                SiteAddress = site.Address ?? "Unknown"
            };

            visits.Add(visit);
        }

        return visits;
    }
    private DateTimeOffset? FindDistributedAvailableDate(
    DateTimeOffset weekStartCandidate,
    ServiceSiteAvailability availability,
    string siteId,
    string serviceId)
    {
        // будуємо список доступних днів у цьому тижні (7 днів від weekStartCandidate)
        var days = new List<DateTimeOffset>();
        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var d = weekStartCandidate.AddDays(dayOffset);
            if (availability.IsAvailableOnDay(d.DayOfWeek))
                days.Add(d);
        }

        if (days.Count == 0) return null;

        // стабільний індекс (щоб не "скакало" щоразу)
        int seed = (siteId + "|" + serviceId).GetHashCode();
        int idx = Math.Abs(seed) % days.Count;

        return days[idx];
    }
    private DateTimeOffset? FindNextAvailableDate(
        DateTimeOffset startDate,
        ServiceSiteAvailability availability)
    {
        // Try to find available day within the week
        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var candidateDate = startDate.AddDays(dayOffset);
            if (availability.IsAvailableOnDay(candidateDate.DayOfWeek))
            {
                return candidateDate;
            }
        }

        return null; // No availability in this week
    }
}
