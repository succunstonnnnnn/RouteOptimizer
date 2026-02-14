using Microsoft.Extensions.Logging;
using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Excel;
using RouteOptimizer.Data.Parsers;
using RouteOptimizer.Data.Preprocessing;
using RouteOptimizer.Data.Validation;

namespace RouteOptimizer.Data;

public class DataProcessor
{
    private readonly ServiceSiteParser _siteParser;
    private readonly TechnicianParser _techParser;
    private readonly VisitGenerator _visitGenerator;
    private readonly DistanceMatrixBuilder _distanceMatrixBuilder;
    private readonly DataValidator _validator;
    private readonly ILogger<DataProcessor>? _logger;

    public DataProcessor(ILogger<DataProcessor>? logger = null)
    {
        _siteParser = new ServiceSiteParser();
        _techParser = new TechnicianParser();
        _visitGenerator = new VisitGenerator();
        _distanceMatrixBuilder = new DistanceMatrixBuilder();
        _validator = new DataValidator();
        _logger = logger;
    }

    public ProcessedData ProcessInputData(
        string sitesJson,
        string techniciansJson,
        DateTimeOffset startDate)
    {
        // Parse raw JSON
        _logger?.LogInformation("Parsing site data...");
        var sites = _siteParser.ParseFromJson(sitesJson);
        _logger?.LogInformation("Parsed {SiteCount} sites", sites.Count);

        _logger?.LogInformation("Parsing technician data...");
        var technicians = _techParser.ParseFromJson(techniciansJson);
        _logger?.LogInformation("Parsed {TechCount} technicians", technicians.Count);

        return ProcessParsedData(sites, technicians, startDate);
    }

    public ProcessedData ProcessFromExcel(
        Stream excelStream,
        DateTimeOffset startDate)
    {
        var reader = new ExcelReader();

        _logger?.LogInformation("Reading site data from Excel...");
        var sites = reader.ReadSites(excelStream);
        _logger?.LogInformation("Read {SiteCount} sites from Excel", sites.Count);

        excelStream.Position = 0;

        _logger?.LogInformation("Reading technician data from Excel...");
        var technicians = reader.ReadTechnicians(excelStream);
        _logger?.LogInformation("Read {TechCount} technicians from Excel", technicians.Count);

        return ProcessParsedData(sites, technicians, startDate);
    }

    private ProcessedData ProcessParsedData(
        List<ServiceSite> sites,
        List<Technician> technicians,
        DateTimeOffset startDate)
    {
        // 1. Validate input
        var inputErrors = _validator.ValidateInput(sites, technicians);
        if (inputErrors.Count > 0)
        {
            _logger?.LogWarning("Input validation found {ErrorCount} issues: {Errors}",
                inputErrors.Count, string.Join("; ", inputErrors));
        }

        // 2. Calculate planning horizon (LCM of frequencies)
        var allServices = sites
            .Where(s => s.Services != null)
            .SelectMany(s => s.Services!)
            .ToList();

        var planningHorizonWeeks = PlanningHorizonCalculator
            .CalculateFromServices(allServices);
        _logger?.LogInformation("Planning horizon: {Weeks} weeks", planningHorizonWeeks);

        // 3. Generate visit instances
        var visits = _visitGenerator.GenerateVisits(sites, startDate, planningHorizonWeeks);
        _logger?.LogInformation("Generated {VisitCount} visit instances", visits.Count);

        // 4. Build distance matrix
        var distanceMatrix = _distanceMatrixBuilder.Build(visits, technicians);
        _logger?.LogInformation("Distance matrix built: {Rows}x{Cols}",
            distanceMatrix.Locations.Count, distanceMatrix.Locations.Count);

        var result = new ProcessedData
        {
            Sites = sites,
            Technicians = technicians,
            Visits = visits,
            DistanceMatrix = distanceMatrix,
            PlanningHorizonWeeks = planningHorizonWeeks,
            StartDate = startDate
        };

        // 5. Validate output
        var outputErrors = _validator.ValidateOutput(result);
        if (outputErrors.Count > 0)
        {
            _logger?.LogWarning("Output validation found {ErrorCount} issues: {Errors}",
                outputErrors.Count, string.Join("; ", outputErrors));
        }

        return result;
    }
}

public class ProcessedData
{
    public List<ServiceSite> Sites { get; set; } = new();
    public List<Technician> Technicians { get; set; } = new();
    public List<VisitInstance> Visits { get; set; } = new();
    public DistanceMatrix DistanceMatrix { get; set; } = new();
    public int PlanningHorizonWeeks { get; set; }
    public DateTimeOffset StartDate { get; set; }
}
