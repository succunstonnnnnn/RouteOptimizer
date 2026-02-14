using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Validation;

public class DataValidator
{
    public List<string> ValidateInput(List<ServiceSite> sites, List<Technician> technicians)
    {
        var errors = new List<string>();

        if (sites.Count == 0)
            errors.Add("No service sites provided.");

        foreach (var site in sites)
        {
            if (string.IsNullOrWhiteSpace(site.Id))
                errors.Add("Site has empty ID.");

            if (site.Coordinates == null)
            {
                errors.Add($"Site '{site.Id}' has no coordinates.");
            }
            else
            {
                if (site.Coordinates.Latitude is < -90 or > 90)
                    errors.Add($"Site '{site.Id}' has invalid latitude: {site.Coordinates.Latitude}");
                if (site.Coordinates.Longitude is < -180 or > 180)
                    errors.Add($"Site '{site.Id}' has invalid longitude: {site.Coordinates.Longitude}");
            }

            if (site.Services == null || site.Services.Count == 0)
                errors.Add($"Site '{site.Id}' has no services.");
            else
            {
                foreach (var service in site.Services)
                {
                    if (string.IsNullOrWhiteSpace(service.Id))
                        errors.Add($"Service in site '{site.Id}' has empty ID.");
                    if ((int)service.VisitFrequency <= 0)
                        errors.Add($"Service '{service.Id}' in site '{site.Id}' has invalid visit frequency.");
                }
            }
        }

        if (technicians.Count == 0)
            errors.Add("No technicians provided.");

        foreach (var tech in technicians)
        {
            if (string.IsNullOrWhiteSpace(tech.Id))
                errors.Add("Technician has empty ID.");

            if (tech.HomeLocation.Latitude == 0 && tech.HomeLocation.Longitude == 0)
                errors.Add($"Technician '{tech.Id}' has default (0,0) home location — likely missing data.");

            if (tech.WorkingDays.Count == 0)
                errors.Add($"Technician '{tech.Id}' has no working days.");

            if (tech.Skills.ServiceSkills.Count == 0)
                errors.Add($"Technician '{tech.Id}' has no service skills.");
        }

        var techIds = technicians.Select(t => t.Id).ToHashSet();
        foreach (var site in sites)
        {
            if (site.Services == null) continue;
            foreach (var service in site.Services)
            {
                if (!string.IsNullOrEmpty(service.TechUserId) && !techIds.Contains(service.TechUserId))
                    errors.Add($"Service '{service.Id}' references technician '{service.TechUserId}' which is not in the technician list.");
            }
        }

        return errors;
    }

    public List<string> ValidateOutput(ProcessedData data)
    {
        var errors = new List<string>();

        if (data.Visits.Count == 0)
            errors.Add("No visit instances were generated.");

        if (data.DistanceMatrix.Locations.Count == 0)
            errors.Add("Distance matrix has no locations.");

        var zeroCoordVisits = data.Visits
            .Where(v => v.Latitude == 0 && v.Longitude == 0)
            .ToList();
        if (zeroCoordVisits.Count > 0)
        {
            var ids = string.Join(", ", zeroCoordVisits.Take(5).Select(v => v.Id));
            errors.Add($"{zeroCoordVisits.Count} visits have (0,0) coordinates: {ids}");
        }

        var duplicateIds = data.Visits
            .GroupBy(v => v.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateIds.Count > 0)
        {
            var ids = string.Join(", ", duplicateIds.Take(5));
            errors.Add($"Duplicate visit IDs found: {ids}");
        }

        var n = data.DistanceMatrix.Locations.Count;
        if (data.DistanceMatrix.Distances.GetLength(0) != n ||
            data.DistanceMatrix.Distances.GetLength(1) != n)
        {
            errors.Add($"Distance matrix dimensions ({data.DistanceMatrix.Distances.GetLength(0)}x{data.DistanceMatrix.Distances.GetLength(1)}) don't match location count ({n}).");
        }

        return errors;
    }
}