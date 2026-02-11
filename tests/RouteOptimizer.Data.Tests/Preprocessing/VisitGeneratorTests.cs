using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;
using RouteOptimizer.Data.Preprocessing;

namespace RouteOptimizer.Data.Tests.Preprocessing;

public class VisitGeneratorTests
{
    private readonly VisitGenerator _generator = new();

    private static ServiceSite MakeSite(string siteId = "site-001", double lat = 40.75, double lon = -73.97)
    {
        var site = new ServiceSite
        {
            Id = siteId,
            Name = "Test Site",
            Address = "123 Test St",
            Coordinates = new Coordinates { Latitude = lat, Longitude = lon },
            Services = new List<Service>()
        };
        site.Availability = ServiceSiteParser.ParseAvailability(site);
        return site;
    }

    private static Service MakeService(
        string serviceId = "svc-001",
        VisitFrequency freq = VisitFrequency.BiWeekly,
        int durationMinutes = 45)
    {
        return new Service
        {
            Id = serviceId,
            SiteId = "site-001",
            JobType = "interior",
            VisitFrequency = freq,
            EstimatedDurationMinutes = durationMinutes,
            IsDeleted = false
        };
    }

    // Monday start date for predictable scheduling
    private static readonly DateTimeOffset StartDate =
        new(2025, 1, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GenerateVisits_BiWeeklyOver4Weeks_GeneratesTwoVisits()
    {
        var site = MakeSite();
        site.Services!.Add(MakeService(freq: VisitFrequency.BiWeekly));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Equal(2, visits.Count);
    }

    [Fact]
    public void GenerateVisits_WeeklyOver4Weeks_GeneratesFourVisits()
    {
        var site = MakeSite();
        site.Services!.Add(MakeService(freq: VisitFrequency.Weekly));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Equal(4, visits.Count);
    }

    [Fact]
    public void GenerateVisits_FourWeeksOver4Weeks_GeneratesOneVisit()
    {
        var site = MakeSite();
        site.Services!.Add(MakeService(freq: VisitFrequency.FourWeeks));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Single(visits);
    }

    [Fact]
    public void GenerateVisits_DeletedService_NoVisits()
    {
        var site = MakeSite();
        var service = MakeService();
        service.IsDeleted = true;
        site.Services!.Add(service);

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Empty(visits);
    }

    [Fact]
    public void GenerateVisits_DeletedSite_NoVisits()
    {
        var site = MakeSite();
        site.IsDeleted = true;
        site.Services!.Add(MakeService());

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Empty(visits);
    }

    [Fact]
    public void GenerateVisits_NullServicesOnSite_NoVisits()
    {
        var site = MakeSite();
        site.Services = null;

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Empty(visits);
    }

    [Fact]
    public void GenerateVisits_CorrectCoordinates()
    {
        var site = MakeSite(lat: 40.1234, lon: -73.5678);
        site.Services!.Add(MakeService(freq: VisitFrequency.FourWeeks));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Single(visits);
        Assert.Equal(40.1234, visits[0].Latitude);
        Assert.Equal(-73.5678, visits[0].Longitude);
    }

    [Fact]
    public void GenerateVisits_NullCoordinates_UsesZero()
    {
        var site = MakeSite();
        site.Coordinates = null;
        site.Services!.Add(MakeService(freq: VisitFrequency.FourWeeks));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Single(visits);
        Assert.Equal(0, visits[0].Latitude);
        Assert.Equal(0, visits[0].Longitude);
    }

    [Fact]
    public void GenerateVisits_VisitIdFormat_Correct()
    {
        var site = MakeSite("site-ABC");
        site.Services!.Add(MakeService("svc-XYZ", VisitFrequency.BiWeekly));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Equal("site-ABC-svc-XYZ-W1", visits[0].Id);
        Assert.Equal("site-ABC-svc-XYZ-W2", visits[1].Id);
    }

    [Fact]
    public void GenerateVisits_DurationPropagated()
    {
        var site = MakeSite();
        site.Services!.Add(MakeService(durationMinutes: 90, freq: VisitFrequency.FourWeeks));

        var visits = _generator.GenerateVisits(new List<ServiceSite> { site }, StartDate, 4);

        Assert.Single(visits);
        Assert.Equal(90, visits[0].DurationMinutes);
    }
}