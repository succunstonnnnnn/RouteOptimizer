namespace RouteOptimizer.Core.Models;

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
