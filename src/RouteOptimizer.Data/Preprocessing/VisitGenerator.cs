using System;
using System.Collections.Generic;
using System.Linq;
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

                var serviceVisits = GenerateVisitsForService(site, service, startDate, planningHorizonWeeks);
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

        var availability = site.Availability;
        var skillsRequired = ServiceSiteParser.InferSkillsRequired(site, service);

        
        if (service.VisitsPerWeek > 0)
        {
            for (int w = 0; w < planningHorizonWeeks; w++)
            {
                var weekStart = startDate.AddDays(w * 7);

                var days = GetAvailableDaysInWeek(weekStart, availability);
                if (days.Count == 0)
                    continue;

                int k = service.VisitsPerWeek;

                var chosenDays = PickKDaysWithRepeatsStable(days, k, site.Id, service.Id, w);

                for (int i = 0; i < chosenDays.Count; i++)
                {
                    var visitDay = chosenDays[i];

                    visits.Add(new VisitInstance
                    {
                        Id = $"{site.Id}-{service.Id}-W{w + 1}-V{i + 1}",
                        ServiceId = service.Id,
                        ServiceSiteId = site.Id,
                        Latitude = site.Coordinates?.Latitude ?? 0,
                        Longitude = site.Coordinates?.Longitude ?? 0,
                        ScheduledDate = visitDay,
                        DurationMinutes = service.EstimatedDurationMinutes,
                        TimeWindows = availability.TimeWindows
                            .Where(tw => tw.DayOfWeek == visitDay.DayOfWeek)
                            .ToList(),
                        SkillsRequired = skillsRequired,
                        AllowedTechnicianIds = service.AllowedTechnicianIds,
                        ForbiddenTechnicianIds = service.ForbiddenTechnicianIds,
                        SecurityClearanceTechnicianIds = site.TechsWithPermit,
                        SiteName = site.Name ?? "Unknown",
                        SiteAddress = site.Address ?? "Unknown"
                    });
                }
            }

            return visits;
        }

        
        var intervalWeeks = (int)service.VisitFrequency;
        if (intervalWeeks <= 0)
            return visits;

        var totalVisits = (int)Math.Ceiling(planningHorizonWeeks / (double)intervalWeeks);

        for (int i = 0; i < totalVisits; i++)
        {
            var scheduledDate = startDate.AddDays(i * intervalWeeks * 7);

            var availableDate = FindDistributedAvailableDate(scheduledDate, availability, site.Id, service.Id);
            if (availableDate == null) continue;

            visits.Add(new VisitInstance
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
            });
        }

        return visits;
    }

    // --- helpers ---

    private static DateTimeOffset? FindDistributedAvailableDate(
        DateTimeOffset weekStartCandidate,
        ServiceSiteAvailability availability,
        string siteId,
        string serviceId)
    {
        var days = GetAvailableDaysInWeek(weekStartCandidate, availability);
        if (days.Count == 0) return null;

        int seed = StableHash(siteId + "|" + serviceId);
        int idx = Math.Abs(seed) % days.Count;

        return days[idx];
    }

    private static List<DateTimeOffset> GetAvailableDaysInWeek(
        DateTimeOffset weekStart,
        ServiceSiteAvailability availability)
    {
        var days = new List<DateTimeOffset>();

        for (int i = 0; i < 7; i++)
        {
            var d = weekStart.AddDays(i);
            if (availability.IsAvailableOnDay(d.DayOfWeek))
                days.Add(d);
        }

        return days;
    }

    private static List<DateTimeOffset> PickKDaysWithRepeatsStable(
        List<DateTimeOffset> days,
        int k,
        string siteId,
        string serviceId,
        int weekIndex)
    {
        int seed = StableHash($"{siteId}|{serviceId}|W{weekIndex}");
        int start = Math.Abs(seed) % days.Count;

        var chosen = new List<DateTimeOffset>();

        for (int i = 0; i < k; i++)
            chosen.Add(days[(start + i) % days.Count]);

        return chosen;
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in s)
                hash = hash * 31 + c;
            return hash;
        }
    }
}
