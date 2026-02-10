using System.Text.Json;
using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Parsers;

public class TechnicianParser
{
    public List<Technician> ParseFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var dtos = JsonSerializer.Deserialize<List<TechnicianDto>>(jsonContent, options)
                   ?? new List<TechnicianDto>();

        return dtos.Select(MapToTechnician).ToList();
    }

    private static Technician MapToTechnician(TechnicianDto dto)
    {
        var tech = new Technician
        {
            Id = dto.Id,
            Name = dto.Name,
            HomeLocation = dto.HomeLocation ?? new Coordinates(),
            OfficeLocation = dto.OfficeLocation,
            StartsFrom = ParseWorkLocation(dto.StartsFrom),
            FinishesAt = ParseWorkLocation(dto.FinishesAt),
            MaxHoursPerDay = dto.MaxHoursPerDay ?? 8,
            MaxHoursPerWeek = dto.MaxHoursPerWeek ?? 40,
            HasVehicle = dto.HasVehicle ?? true,
            Skills = ParseSkills(dto),
            BreakRequirement = ParseBreakRequirement(dto)
        };

        var dayMappings = new (DayOfWeek Day, string? Start, string? End)[]
        {
            (DayOfWeek.Monday, dto.MondayStart, dto.MondayEnd),
            (DayOfWeek.Tuesday, dto.TuesdayStart, dto.TuesdayEnd),
            (DayOfWeek.Wednesday, dto.WednesdayStart, dto.WednesdayEnd),
            (DayOfWeek.Thursday, dto.ThursdayStart, dto.ThursdayEnd),
            (DayOfWeek.Friday, dto.FridayStart, dto.FridayEnd),
            (DayOfWeek.Saturday, dto.SaturdayStart, dto.SaturdayEnd),
            (DayOfWeek.Sunday, dto.SundayStart, dto.SundayEnd),
        };

        foreach (var (day, start, end) in dayMappings)
        {
            var startTime = ParseTimeSpan(start);
            var endTime = ParseTimeSpan(end);

            if (startTime.HasValue && endTime.HasValue)
            {
                tech.DailySchedule[day] = (startTime.Value, endTime.Value);
                tech.WorkingDays.Add(day);
            }
        }

        return tech;
    }

    internal static WorkLocation ParseWorkLocation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return WorkLocation.Home;

        return value.Trim().ToLowerInvariant() switch
        {
            "home" => WorkLocation.Home,
            "office" => WorkLocation.Office,
            "either" or "either works" => WorkLocation.Either,
            _ => WorkLocation.Home
        };
    }

    internal static TechnicianSkills ParseSkills(TechnicianDto dto)
    {
        return new TechnicianSkills
        {
            ServiceSkills = ParseServiceSkillsString(dto.ServiceSkills),
            CanDoPhysicallyDemanding = dto.CanDoPhysicallyDemanding ?? false,
            IsSkilledInLivingWalls = dto.IsSkilledInLivingWalls ?? false,
            IsComfortableWithHeights = dto.IsComfortableWithHeights ?? false,
            HasLiftCertification = dto.HasLiftCertification ?? false,
            HasPesticideCertification = dto.HasPesticideCertification ?? false,
            IsCitizen = dto.IsCitizen ?? false,
        };
    }

    internal static List<ServiceSkill> ParseServiceSkillsString(string? skillsStr)
    {
        var result = new List<ServiceSkill>();
        if (string.IsNullOrWhiteSpace(skillsStr)) return result;

        var parts = skillsStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var segments = part.Split('-', StringSplitOptions.TrimEntries);
            if (segments.Length != 2) continue;

            var serviceType = segments[0].ToLowerInvariant() switch
            {
                "interior" => (ServiceType?)ServiceType.Interior,
                "exterior" => (ServiceType?)ServiceType.Exterior,
                "floral" => (ServiceType?)ServiceType.Floral,
                _ => null
            };

            var skillLevel = segments[1].ToLowerInvariant() switch
            {
                "junior" => (SkillLevel?)SkillLevel.Junior,
                "medior" => (SkillLevel?)SkillLevel.Medior,
                "senior" => (SkillLevel?)SkillLevel.Senior,
                _ => null
            };

            if (serviceType.HasValue && skillLevel.HasValue)
            {
                result.Add(new ServiceSkill
                {
                    ServiceType = serviceType.Value,
                    SkillLevel = skillLevel.Value
                });
            }
        }

        return result;
    }

    private static BreakRequirement ParseBreakRequirement(TechnicianDto dto)
    {
        return new BreakRequirement
        {
            MinBreakMinutes = dto.MinBreakMinutes ?? 30,
            BreakWindowStart = ParseTimeSpan(dto.BreakWindowStart) ?? new TimeSpan(12, 0, 0),
            BreakWindowEnd = ParseTimeSpan(dto.BreakWindowEnd) ?? new TimeSpan(14, 0, 0)
        };
    }

    private static TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeSpan.TryParse(value, out var result) ? result : null;
    }
}