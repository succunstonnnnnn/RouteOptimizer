using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Tests;

public class ConstraintMatchingTests
{
    [Fact]
    public void Matches_BasicSkillMatch_ReturnsTrue()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Interior,
            MinimumSkillLevel = SkillLevel.Junior
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Medior }
            }
        };

        Assert.True(required.Matches(techSkills));
    }

    [Fact]
    public void Matches_InsufficientSkillLevel_ReturnsFalse()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Interior,
            MinimumSkillLevel = SkillLevel.Senior
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Medior }
            }
        };

        Assert.False(required.Matches(techSkills));
    }

    [Fact]
    public void Matches_WrongServiceType_ReturnsFalse()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Exterior,
            MinimumSkillLevel = SkillLevel.Junior
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Senior }
            }
        };

        Assert.False(required.Matches(techSkills));
    }

    [Fact]
    public void Matches_PhysicallyDemanding_RequiresCapability()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Exterior,
            MinimumSkillLevel = SkillLevel.Junior,
            IsPhysicallyDemanding = true
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Exterior, SkillLevel = SkillLevel.Junior }
            },
            CanDoPhysicallyDemanding = false
        };

        Assert.False(required.Matches(techSkills));
    }

    [Fact]
    public void Matches_AllConstraintsRequired_AllMet_ReturnsTrue()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Interior,
            MinimumSkillLevel = SkillLevel.Medior,
            IsPhysicallyDemanding = true,
            RequiresLivingWalls = true,
            RequiresHeightWork = true,
            RequiresLift = true,
            RequiresPesticideCertification = true,
            RequiresCitizenship = true
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Senior }
            },
            CanDoPhysicallyDemanding = true,
            IsSkilledInLivingWalls = true,
            IsComfortableWithHeights = true,
            HasLiftCertification = true,
            HasPesticideCertification = true,
            IsCitizen = true
        };

        Assert.True(required.Matches(techSkills));
    }

    [Fact]
    public void Matches_CitizenshipRequired_NotCitizen_ReturnsFalse()
    {
        var required = new SkillsRequired
        {
            ServiceType = ServiceType.Interior,
            MinimumSkillLevel = SkillLevel.Junior,
            RequiresCitizenship = true
        };
        var techSkills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Junior }
            },
            IsCitizen = false
        };

        Assert.False(required.Matches(techSkills));
    }

    [Fact]
    public void InferSkillsRequired_MapsConstraintFieldsFromService()
    {
        var parser = new ServiceSiteParser();
        var site = new ServiceSite { Id = "s1", BestAccessedBy = TransportType.Car };
        var service = new Service
        {
            Id = "svc1",
            JobType = "exterior",
            PhysicallyDemanding = true,
            RequiresLivingWalls = true,
            RequiresHeightWork = false,
            RequiresLift = true,
            RequiresPesticides = false,
            RequiresCitizen = true
        };

        var result = parser.InferSkillsRequired(site, service);

        Assert.Equal(ServiceType.Exterior, result.ServiceType);
        Assert.True(result.IsPhysicallyDemanding);
        Assert.True(result.RequiresLivingWalls);
        Assert.False(result.RequiresHeightWork);
        Assert.True(result.RequiresLift);
        Assert.False(result.RequiresPesticideCertification);
        Assert.True(result.RequiresCitizenship);
        Assert.Equal(TransportType.Car, result.PreferredTransport);
    }
}