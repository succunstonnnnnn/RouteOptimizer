using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Validation;

namespace RouteOptimizer.Data.Tests.Validation;

public class DataValidatorTests
{
    private readonly DataValidator _validator = new();

    private static Technician MakeTech(string id = "t1") => new()
    {
        Id = id,
        HomeLocation = new Coordinates { Latitude = 40.75, Longitude = -73.97 },
        WorkingDays = { DayOfWeek.Monday },
        Skills = new TechnicianSkills
        {
            ServiceSkills = new List<ServiceSkill>
            {
                new() { ServiceType = ServiceType.Interior, SkillLevel = SkillLevel.Junior }
            }
        }
    };

    private static ServiceSite MakeSite(string id = "s1") => new()
    {
        Id = id,
        Coordinates = new Coordinates { Latitude = 40.75, Longitude = -73.97 },
        Services = new List<Service> { new() { Id = "svc1", VisitFrequency = VisitFrequency.BiWeekly } }
    };

    [Fact]
    public void ValidateInput_EmptySites_ReportsError()
    {
        var errors = _validator.ValidateInput(new List<ServiceSite>(), new List<Technician> { MakeTech() });
        Assert.Contains(errors, e => e.Contains("No service sites"));
    }

    [Fact]
    public void ValidateInput_EmptyTechnicians_ReportsError()
    {
        var errors = _validator.ValidateInput(new List<ServiceSite> { MakeSite() }, new List<Technician>());
        Assert.Contains(errors, e => e.Contains("No technicians"));
    }

    [Fact]
    public void ValidateInput_MissingCoordinates_ReportsError()
    {
        var site = MakeSite();
        site.Coordinates = null;

        var errors = _validator.ValidateInput(new List<ServiceSite> { site }, new List<Technician> { MakeTech() });
        Assert.Contains(errors, e => e.Contains("no coordinates"));
    }

    [Fact]
    public void ValidateInput_InvalidLatitude_ReportsError()
    {
        var site = MakeSite();
        site.Coordinates = new Coordinates { Latitude = 100, Longitude = -73 };

        var errors = _validator.ValidateInput(new List<ServiceSite> { site }, new List<Technician> { MakeTech() });
        Assert.Contains(errors, e => e.Contains("invalid latitude"));
    }

    [Fact]
    public void ValidateInput_NoServices_ReportsError()
    {
        var site = MakeSite();
        site.Services = null;

        var errors = _validator.ValidateInput(new List<ServiceSite> { site }, new List<Technician> { MakeTech() });
        Assert.Contains(errors, e => e.Contains("no services"));
    }

    [Fact]
    public void ValidateInput_CrossRefMismatch_ReportsError()
    {
        var site = MakeSite();
        site.Services![0].TechUserId = "nonexistent-tech";

        var errors = _validator.ValidateInput(new List<ServiceSite> { site }, new List<Technician> { MakeTech() });
        Assert.Contains(errors, e => e.Contains("nonexistent-tech"));
    }

    [Fact]
    public void ValidateInput_ValidData_NoErrors()
    {
        var errors = _validator.ValidateInput(
            new List<ServiceSite> { MakeSite() },
            new List<Technician> { MakeTech() });

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateOutput_ZeroCoordinateVisits_ReportsWarning()
    {
        var data = new ProcessedData
        {
            Visits = new List<VisitInstance> { new() { Id = "v1", Latitude = 0, Longitude = 0 } },
            DistanceMatrix = new DistanceMatrix
            {
                Locations = new List<Location> { new() { Id = "loc1" } },
                Distances = new double[1, 1]
            }
        };

        var errors = _validator.ValidateOutput(data);
        Assert.Contains(errors, e => e.Contains("(0,0) coordinates"));
    }

    [Fact]
    public void ValidateOutput_DuplicateVisitIds_ReportsError()
    {
        var data = new ProcessedData
        {
            Visits = new List<VisitInstance>
            {
                new() { Id = "v1", Latitude = 40, Longitude = -73 },
                new() { Id = "v1", Latitude = 40, Longitude = -73 }
            },
            DistanceMatrix = new DistanceMatrix
            {
                Locations = new List<Location> { new() { Id = "loc1" } },
                Distances = new double[1, 1]
            }
        };

        var errors = _validator.ValidateOutput(data);
        Assert.Contains(errors, e => e.Contains("Duplicate visit IDs"));
    }
}
