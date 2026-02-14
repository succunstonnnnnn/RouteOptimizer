using RouteOptimizer.Data.Excel;
using RouteOptimizer.Data.Parsers;
using Xunit.Abstractions;

namespace RouteOptimizer.Data.Tests;

public class ExcelPipelineTests
{
    private readonly ITestOutputHelper _output;

    public ExcelPipelineTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ReadSites_FromRealExcel_ParsesAllSites()
    {
        var path = Path.Combine(TestHelper.SamplesPath, "ServiceSites.xlsx");
        using var stream = File.OpenRead(path);

        var reader = new ExcelReader();
        var sites = reader.ReadSites(stream);

        _output.WriteLine($"Total sites parsed: {sites.Count}");

        foreach (var site in sites.Take(5))
        {
            _output.WriteLine($"  {site.Id}: {site.Name}");
            _output.WriteLine($"    Address: {site.Address}");
            _output.WriteLine($"    Transport: {site.BestAccessedBy}, Permit: {site.RequiresPermit}");
            _output.WriteLine($"    Availability windows: {site.Availability?.TimeWindows.Count ?? 0} days");

            if (site.Services != null)
            {
                foreach (var svc in site.Services)
                {
                    _output.WriteLine($"    Service: {svc.Id} | {svc.JobType} | freq={svc.FrequencyOfVisits} ({svc.VisitFrequency}) | {svc.EstimatedDurationMinutes}min");
                    _output.WriteLine($"      Tech: {svc.TechUserName ?? svc.TechUserId ?? "(none)"}");
                }
            }
        }

        Assert.True(sites.Count > 0, "Should parse at least one site");
        Assert.All(sites, s => Assert.False(string.IsNullOrEmpty(s.Id)));
        Assert.All(sites, s => Assert.NotNull(s.Services));
    }

    [Fact]
    public void ReadTechnicians_FromRealExcel_ParsesAllTechnicians()
    {
        var path = Path.Combine(TestHelper.SamplesPath, "Technicians.xlsx");
        using var stream = File.OpenRead(path);

        var reader = new ExcelReader();
        var technicians = reader.ReadTechnicians(stream);

        _output.WriteLine($"Total technicians parsed: {technicians.Count}");

        foreach (var tech in technicians.Take(5))
        {
            _output.WriteLine($"  {tech.Id}: {tech.Name}");
            _output.WriteLine($"    Starts: {tech.StartsFrom}, Finishes: {tech.FinishesAt}");
            _output.WriteLine($"    Working days: {string.Join(", ", tech.WorkingDays)}");
            _output.WriteLine($"    Max hours: {tech.MaxHoursPerDay}h/day, {tech.MaxHoursPerWeek}h/week");
            _output.WriteLine($"    Skills: {string.Join(", ", tech.Skills.ServiceSkills)}");
            _output.WriteLine($"    Break: {tech.BreakRequirement.MinBreakMinutes}min ({tech.BreakRequirement.BreakWindowStart}-{tech.BreakRequirement.BreakWindowEnd})");
        }

        Assert.True(technicians.Count > 0, "Should parse at least one technician");
        Assert.All(technicians, t => Assert.False(string.IsNullOrEmpty(t.Id)));
        Assert.All(technicians, t => Assert.False(string.IsNullOrEmpty(t.Name)));
    }

    [Fact]
    public void ProcessFromExcel_FullPipeline_ProducesProcessedData()
    {
        var path = Path.Combine(TestHelper.SamplesPath, "ServiceSites.xlsx");
        using var stream = File.OpenRead(path);

        var processor = new DataProcessor();
        var result = processor.ProcessFromExcel(stream, DateTimeOffset.Now);

        _output.WriteLine($"Sites: {result.Sites.Count}");
        _output.WriteLine($"Technicians: {result.Technicians.Count}");
        _output.WriteLine($"Visits generated: {result.Visits.Count}");
        _output.WriteLine($"Planning horizon: {result.PlanningHorizonWeeks} weeks");
        _output.WriteLine($"Distance matrix: {result.DistanceMatrix.Locations.Count} locations");

        Assert.True(result.Sites.Count > 0);
        Assert.True(result.Technicians.Count > 0);
        Assert.True(result.Visits.Count > 0);
        Assert.True(result.DistanceMatrix.Locations.Count > 0);
    }
}
