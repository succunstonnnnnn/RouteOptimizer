using Microsoft.Extensions.Logging;
using RouteOptimizer.Core.Models;
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
        _visitGenerator = new VisitGenerator(_siteParser);
        _distanceMatrixBuilder = new DistanceMatrixBuilder();
        _validator = new DataValidator();
        _logger = logger;
    }

    public ProcessedData ProcessInputData(
        string sitesJson,
        string techniciansJson,
        DateTimeOffset startDate)
    {
        // 1. Parse raw JSON
        _logger?.LogInformation("Parsing site data...");
        var sites = _siteParser.ParseFromJson(sitesJson);
        _logger?.LogInformation("Parsed {SiteCount} sites", sites.Count);

        _logger?.LogInformation("Parsing technician data...");
        var technicians = _techParser.ParseFromJson(techniciansJson);
        _logger?.LogInformation("Parsed {TechCount} technicians", technicians.Count);

        // 2. Validate input
        var inputErrors = _validator.ValidateInput(sites, technicians);
        if (inputErrors.Count > 0)
        {
            _logger?.LogWarning("Input validation found {ErrorCount} issues: {Errors}",
                inputErrors.Count, string.Join("; ", inputErrors));
        }

        // 3. Calculate planning horizon (LCM of frequencies)
        var allServices = sites
            .Where(s => s.Services != null)
            .SelectMany(s => s.Services!)
            .ToList();

        var planningHorizonWeeks = PlanningHorizonCalculator
            .CalculateFromServices(allServices);
        _logger?.LogInformation("Planning horizon: {Weeks} weeks", planningHorizonWeeks);

        // 4. Generate visit instances
        var visits = _visitGenerator.GenerateVisits(sites, startDate, planningHorizonWeeks);
        _logger?.LogInformation("Generated {VisitCount} visit instances", visits.Count);

        // 5. Build distance matrix
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

        // 6. Validate output
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
