using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Tests.Parsers;

public class ServiceSiteParserTests
{
    private readonly ServiceSiteParser _parser = new();

    [Fact]
    public void ParseFromJson_SampleData_ParsesAllSites()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-sites.json"));
        var sites = _parser.ParseFromJson(json);

        Assert.Equal(3, sites.Count);
    }

    [Fact]
    public void ParseFromJson_SiteCoordinates_Correct()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-sites.json"));
        var sites = _parser.ParseFromJson(json);

        var site1 = sites.First(s => s.Id == "site-001");
        Assert.NotNull(site1.Coordinates);
        Assert.Equal(40.7489, site1.Coordinates!.Latitude);
        Assert.Equal(-73.9680, site1.Coordinates.Longitude);
    }

    [Fact]
    public void ParseFromJson_FrequencyStringsParsed()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-sites.json"));
        var sites = _parser.ParseFromJson(json);

        var svc1 = sites[0].Services![0];
        Assert.Equal(VisitFrequency.BiWeekly, svc1.VisitFrequency); // "2 weeks"

        var svc2 = sites[1].Services![0];
        Assert.Equal(VisitFrequency.ThreeWeekly, svc2.VisitFrequency); // "3 weeks"
    }

    [Theory]
    [InlineData("1 week", VisitFrequency.Weekly)]
    [InlineData("weekly", VisitFrequency.Weekly)]
    [InlineData("2 weeks", VisitFrequency.BiWeekly)]
    [InlineData("biweekly", VisitFrequency.BiWeekly)]
    [InlineData("bi-weekly", VisitFrequency.BiWeekly)]
    [InlineData("3 weeks", VisitFrequency.ThreeWeekly)]
    [InlineData("4 weeks", VisitFrequency.FourWeeks)]
    [InlineData("monthly", VisitFrequency.FourWeeks)]
    [InlineData(null, VisitFrequency.BiWeekly)]
    [InlineData("", VisitFrequency.BiWeekly)]
    [InlineData("unknown", VisitFrequency.BiWeekly)]
    public void ParseFrequencyString_VariousInputs(string? input, VisitFrequency expected)
    {
        Assert.Equal(expected, ServiceSiteParser.ParseFrequencyString(input));
    }

    [Fact]
    public void ParseFromJson_MissingEstimatedDuration_GetsDefault()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-sites.json"));
        var sites = _parser.ParseFromJson(json);

        foreach (var site in sites)
        {
            foreach (var service in site.Services!)
            {
                Assert.True(service.EstimatedDurationMinutes > 0,
                    $"Service {service.Id} should have a non-zero estimated duration.");
            }
        }
    }

    [Fact]
    public void ParseFromJson_ServiceIds_Preserved()
    {
        var json = File.ReadAllText(Path.Combine(TestHelper.SamplesPath, "sample-sites.json"));
        var sites = _parser.ParseFromJson(json);

        Assert.Equal("service-001", sites[0].Services![0].Id);
        Assert.Equal("service-002", sites[1].Services![0].Id);
        Assert.Equal("service-003", sites[2].Services![0].Id);
    }
}