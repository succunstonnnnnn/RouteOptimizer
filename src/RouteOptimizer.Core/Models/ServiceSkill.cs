namespace RouteOptimizer.Core.Models;

public class ServiceSkill
{
    public ServiceType ServiceType { get; set; }
    public SkillLevel SkillLevel { get; set; }

    public override string ToString() => $"{ServiceType} - {SkillLevel}";
}


