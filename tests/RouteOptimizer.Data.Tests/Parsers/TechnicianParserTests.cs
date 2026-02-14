using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Tests.Parsers;

public class TechnicianParserTests
{
    private readonly TechnicianParser _parser = new();

    [Fact]
    public void ParseFromJson_SampleData_ParsesAllTechnicians()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);

        Assert.Equal(3, techs.Count);
    }

    [Fact]
    public void ParseFromJson_MaryPoppins_CorrectSchedule()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);
        var mary = techs.First(t => t.Id == "tech-001");

        Assert.Equal("Mary Poppins", mary.Name);
        Assert.Equal(5, mary.WorkingDays.Count);
        Assert.Contains(DayOfWeek.Monday, mary.WorkingDays);
        Assert.DoesNotContain(DayOfWeek.Saturday, mary.WorkingDays);
        Assert.Equal(new TimeSpan(8, 0, 0), mary.DailySchedule[DayOfWeek.Monday].Start);
        Assert.Equal(new TimeSpan(17, 0, 0), mary.DailySchedule[DayOfWeek.Monday].End);
    }

    [Fact]
    public void ParseFromJson_StartsFromHome_FinishesAtOffice()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);
        var mary = techs.First(t => t.Id == "tech-001");

        Assert.Equal(WorkLocation.Home, mary.StartsFrom);
        Assert.Equal(WorkLocation.Office, mary.FinishesAt);
    }

    [Fact]
    public void ParseWorkLocation_EitherWorks_ReturnsEither()
    {
        Assert.Equal(WorkLocation.Either, TechnicianParser.ParseWorkLocation("either works"));
        Assert.Equal(WorkLocation.Either, TechnicianParser.ParseWorkLocation("either"));
    }

    [Fact]
    public void ParseWorkLocation_NullOrEmpty_ReturnsHome()
    {
        Assert.Equal(WorkLocation.Home, TechnicianParser.ParseWorkLocation(null));
        Assert.Equal(WorkLocation.Home, TechnicianParser.ParseWorkLocation(""));
    }

    [Fact]
    public void ParseServiceSkillsString_MultipleSkills_ParsesCorrectly()
    {
        var skills = TechnicianParser.ParseServiceSkillsString("interior - medior, exterior - medior");

        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.ServiceType == ServiceType.Interior && s.SkillLevel == SkillLevel.Medior);
        Assert.Contains(skills, s => s.ServiceType == ServiceType.Exterior && s.SkillLevel == SkillLevel.Medior);
    }

    [Fact]
    public void ParseServiceSkillsString_SingleSkill_ParsesCorrectly()
    {
        var skills = TechnicianParser.ParseServiceSkillsString("interior - senior");

        Assert.Single(skills);
        Assert.Equal(ServiceType.Interior, skills[0].ServiceType);
        Assert.Equal(SkillLevel.Senior, skills[0].SkillLevel);
    }

    [Fact]
    public void ParseServiceSkillsString_EmptyOrNull_ReturnsEmptyList()
    {
        Assert.Empty(TechnicianParser.ParseServiceSkillsString(null));
        Assert.Empty(TechnicianParser.ParseServiceSkillsString(""));
        Assert.Empty(TechnicianParser.ParseServiceSkillsString("  "));
    }

    [Fact]
    public void ParseFromJson_SkillCapabilities_MappedCorrectly()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);

        var mary = techs.First(t => t.Id == "tech-001");
        Assert.True(mary.Skills.CanDoPhysicallyDemanding);
        Assert.True(mary.Skills.IsSkilledInLivingWalls);
        Assert.True(mary.Skills.IsComfortableWithHeights);
        Assert.True(mary.Skills.HasLiftCertification);
        Assert.True(mary.Skills.HasPesticideCertification);
        Assert.True(mary.Skills.IsCitizen);

        var anne = techs.First(t => t.Id == "tech-003");
        Assert.False(anne.Skills.CanDoPhysicallyDemanding);
        Assert.False(anne.Skills.HasLiftCertification);
        Assert.False(anne.Skills.IsCitizen);
    }

    [Fact]
    public void ParseFromJson_BreakRequirement_Correct()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);
        var mary = techs.First(t => t.Id == "tech-001");

        Assert.Equal(30, mary.BreakRequirement.MinBreakMinutes);
        Assert.Equal(new TimeSpan(12, 0, 0), mary.BreakRequirement.BreakWindowStart);
        Assert.Equal(new TimeSpan(14, 0, 0), mary.BreakRequirement.BreakWindowEnd);
    }

    [Fact]
    public void ParseFromJson_HomeAndOfficeLocations()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-technicians.json"));
        var techs = _parser.ParseFromJson(json);

        var mary = techs.First(t => t.Id == "tech-001");
        Assert.Equal(40.7589, mary.HomeLocation.Latitude);
        Assert.Equal(-73.9851, mary.HomeLocation.Longitude);
        Assert.NotNull(mary.OfficeLocation);
        Assert.Equal(40.7489, mary.OfficeLocation!.Latitude);

        var tom = techs.First(t => t.Id == "tech-002");
        Assert.Null(tom.OfficeLocation);
    }
}