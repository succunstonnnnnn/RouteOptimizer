namespace RouteOptimizer.Core.Models;

public class ServiceSkill
{
    public ServiceType ServiceType { get; set; }
    public SkillLevel SkillLevel { get; set; }

    public override string ToString() => $"{ServiceType} - {SkillLevel}";
}

public class SkillsRequired
{
    public ServiceType ServiceType { get; set; }
    public SkillLevel MinimumSkillLevel { get; set; }

    public bool IsPhysicallyDemanding { get; set; }
    public bool RequiresLivingWalls { get; set; }
    public bool RequiresHeightWork { get; set; }

    public bool RequiresLift { get; set; }
    public bool RequiresPesticideCertification { get; set; }
    public bool RequiresCitizenship { get; set; }


    public TransportType PreferredTransport { get; set; } = TransportType.Either;

    public bool Matches(TechnicianSkills techSkills)
    {
        var hasSkill = techSkills.ServiceSkills.Any(s =>
            s.ServiceType == ServiceType &&
            s.SkillLevel >= MinimumSkillLevel
        );

        if (!hasSkill)
            return false;

        if (IsPhysicallyDemanding && !techSkills.CanDoPhysicallyDemanding)
            return false;

        if (RequiresLivingWalls && !techSkills.IsSkilledInLivingWalls)
            return false;

        if (RequiresHeightWork && !techSkills.IsComfortableWithHeights)
            return false;

        if (RequiresLift && !techSkills.HasLiftCertification)
            return false;

        if (RequiresPesticideCertification && !techSkills.HasPesticideCertification)
            return false;

        if (RequiresCitizenship && !techSkills.IsCitizen)
            return false;

        return true;
    }
}

public class TechnicianSkills
{
    public List<ServiceSkill> ServiceSkills { get; set; } = new();

    public bool CanDoPhysicallyDemanding { get; set; }
    public bool IsSkilledInLivingWalls { get; set; }
    public bool IsComfortableWithHeights { get; set; }

    public bool HasLiftCertification { get; set; }
    public bool HasPesticideCertification { get; set; }
    public bool IsCitizen { get; set; }

    public bool HasSkill(ServiceType serviceType, SkillLevel minimumLevel)
    {
        return ServiceSkills.Any(s =>
            s.ServiceType == serviceType &&
            s.SkillLevel >= minimumLevel
        );
    }
}
