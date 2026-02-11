using ClosedXML.Excel;
using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Excel;

public class ExcelReader
{
    public List<ServiceSite> ReadSites(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("Sites");
        var sites = new List<ServiceSite>();

        var columns = BuildColumnMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var xlRow = ws.Row(row);
            var id = GetCellString(xlRow, columns, "Id");
            if (string.IsNullOrWhiteSpace(id)) break;

            var site = new ServiceSite
            {
                Id = id,
                Name = GetCellString(xlRow, columns, "Name"),
                Address = GetCellString(xlRow, columns, "Address"),
                Coordinates = new Coordinates
                {
                    Latitude = GetCellDouble(xlRow, columns, "Latitude"),
                    Longitude = GetCellDouble(xlRow, columns, "Longitude")
                },
                City = GetCellString(xlRow, columns, "City"),
                ZipCode = GetCellString(xlRow, columns, "ZipCode"),
                TenantId = GetCellString(xlRow, columns, "TenantId") ?? string.Empty,
                BestAccessedBy = ParseTransportType(GetCellString(xlRow, columns, "TransportType")),
                RequiresPermit = GetCellBool(xlRow, columns, "RequiresPermit"),
                PermitDifficulty = ParsePermitDifficulty(GetCellString(xlRow, columns, "PermitDifficulty")),
                TechsWithPermit = ParseCommaSeparatedList(GetCellString(xlRow, columns, "TechsWithPermit")),
                MustBeServicedWithSiteIds = ParseCommaSeparatedList(GetCellString(xlRow, columns, "MustBeServicedWithSiteIds")),
                MondayStart = GetCellString(xlRow, columns, "MondayStart"),
                MondayEnd = GetCellString(xlRow, columns, "MondayEnd"),
                TuesdayStart = GetCellString(xlRow, columns, "TuesdayStart"),
                TuesdayEnd = GetCellString(xlRow, columns, "TuesdayEnd"),
                WednesdayStart = GetCellString(xlRow, columns, "WednesdayStart"),
                WednesdayEnd = GetCellString(xlRow, columns, "WednesdayEnd"),
                ThursdayStart = GetCellString(xlRow, columns, "ThursdayStart"),
                ThursdayEnd = GetCellString(xlRow, columns, "ThursdayEnd"),
                FridayStart = GetCellString(xlRow, columns, "FridayStart"),
                FridayEnd = GetCellString(xlRow, columns, "FridayEnd"),
                SaturdayStart = GetCellString(xlRow, columns, "SaturdayStart"),
                SaturdayEnd = GetCellString(xlRow, columns, "SaturdayEnd"),
                SundayStart = GetCellString(xlRow, columns, "SundayStart"),
                SundayEnd = GetCellString(xlRow, columns, "SundayEnd"),
            };

            site.Availability = ServiceSiteParser.ParseAvailability(site);
            sites.Add(site);
        }

        if (workbook.TryGetWorksheet("Services", out var servicesWs))
            ReadServicesIntoSites(servicesWs, sites);

        return sites;
    }

    public List<Technician> ReadTechnicians(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("Technicians");
        var technicians = new List<Technician>();

        var columns = BuildColumnMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var xlRow = ws.Row(row);
            var id = GetCellString(xlRow, columns, "Id");
            if (string.IsNullOrWhiteSpace(id)) break;

            var dto = new TechnicianDto
            {
                Id = id,
                Name = GetCellString(xlRow, columns, "Name") ?? string.Empty,
                HomeLocation = new Coordinates
                {
                    Latitude = GetCellDouble(xlRow, columns, "HomeLat"),
                    Longitude = GetCellDouble(xlRow, columns, "HomeLon")
                },
                StartsFrom = GetCellString(xlRow, columns, "StartsFrom"),
                FinishesAt = GetCellString(xlRow, columns, "FinishesAt"),
                MondayStart = GetCellString(xlRow, columns, "MondayStart"),
                MondayEnd = GetCellString(xlRow, columns, "MondayEnd"),
                TuesdayStart = GetCellString(xlRow, columns, "TuesdayStart"),
                TuesdayEnd = GetCellString(xlRow, columns, "TuesdayEnd"),
                WednesdayStart = GetCellString(xlRow, columns, "WednesdayStart"),
                WednesdayEnd = GetCellString(xlRow, columns, "WednesdayEnd"),
                ThursdayStart = GetCellString(xlRow, columns, "ThursdayStart"),
                ThursdayEnd = GetCellString(xlRow, columns, "ThursdayEnd"),
                FridayStart = GetCellString(xlRow, columns, "FridayStart"),
                FridayEnd = GetCellString(xlRow, columns, "FridayEnd"),
                SaturdayStart = GetCellString(xlRow, columns, "SaturdayStart"),
                SaturdayEnd = GetCellString(xlRow, columns, "SaturdayEnd"),
                SundayStart = GetCellString(xlRow, columns, "SundayStart"),
                SundayEnd = GetCellString(xlRow, columns, "SundayEnd"),
                MinBreakMinutes = GetCellInt(xlRow, columns, "MinBreakMinutes"),
                BreakWindowStart = GetCellString(xlRow, columns, "BreakWindowStart"),
                BreakWindowEnd = GetCellString(xlRow, columns, "BreakWindowEnd"),
                MaxHoursPerDay = GetCellInt(xlRow, columns, "MaxHoursPerDay"),
                MaxHoursPerWeek = GetCellInt(xlRow, columns, "MaxHoursPerWeek"),
                ServiceSkills = GetCellString(xlRow, columns, "ServiceSkills"),
                CanDoPhysicallyDemanding = GetCellBoolNullable(xlRow, columns, "CanDoPhysicallyDemanding"),
                IsSkilledInLivingWalls = GetCellBoolNullable(xlRow, columns, "IsSkilledInLivingWalls"),
                IsComfortableWithHeights = GetCellBoolNullable(xlRow, columns, "IsComfortableWithHeights"),
                HasLiftCertification = GetCellBoolNullable(xlRow, columns, "HasLiftCertification"),
                HasPesticideCertification = GetCellBoolNullable(xlRow, columns, "HasPesticideCertification"),
                IsCitizen = GetCellBoolNullable(xlRow, columns, "IsCitizen"),
                HasVehicle = GetCellBoolNullable(xlRow, columns, "HasVehicle"),
            };

            if (columns.ContainsKey("OfficeLat") && columns.ContainsKey("OfficeLon"))
            {
                var officeLat = GetCellDouble(xlRow, columns, "OfficeLat");
                var officeLon = GetCellDouble(xlRow, columns, "OfficeLon");
                if (officeLat != 0 || officeLon != 0)
                    dto.OfficeLocation = new Coordinates { Latitude = officeLat, Longitude = officeLon };
            }

            var parser = new TechnicianParser();
            technicians.AddRange(parser.ParseFromJson(
                System.Text.Json.JsonSerializer.Serialize(new[] { dto })));
        }

        return technicians;
    }

    private void ReadServicesIntoSites(IXLWorksheet ws, List<ServiceSite> sites)
    {
        var siteIndex = sites.ToDictionary(s => s.Id);
        var columns = BuildColumnMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var xlRow = ws.Row(row);
            var siteId = GetCellString(xlRow, columns, "SiteId");
            if (string.IsNullOrWhiteSpace(siteId)) break;
            if (!siteIndex.TryGetValue(siteId, out var site)) continue;

            var service = new Service
            {
                Id = GetCellString(xlRow, columns, "Id") ?? string.Empty,
                SiteId = siteId,
                JobType = GetCellString(xlRow, columns, "JobType"),
                FrequencyOfVisits = GetCellString(xlRow, columns, "FrequencyOfVisits"),
                EstimatedDurationMinutes = GetCellInt(xlRow, columns, "EstimatedDurationMinutes") ?? 0,
                TechUserId = GetCellString(xlRow, columns, "TechUserId"),
                PhysicallyDemanding = GetCellBool(xlRow, columns, "PhysicallyDemanding"),
                RequiresLivingWalls = GetCellBool(xlRow, columns, "RequiresLivingWalls"),
                RequiresHeightWork = GetCellBool(xlRow, columns, "RequiresHeightWork"),
                RequiresLift = GetCellBool(xlRow, columns, "RequiresLift"),
                RequiresPesticides = GetCellBool(xlRow, columns, "RequiresPesticides"),
                RequiresCitizen = GetCellBool(xlRow, columns, "RequiresCitizen"),
                AllowedTechnicianIds = ParseCommaSeparatedList(GetCellString(xlRow, columns, "AllowedTechnicianIds")),
                ForbiddenTechnicianIds = ParseCommaSeparatedList(GetCellString(xlRow, columns, "ForbiddenTechnicianIds")),
            };

            service.VisitFrequency = ServiceSiteParser.ParseFrequencyString(service.FrequencyOfVisits);

            site.Services ??= new List<Service>();
            site.Services.Add(service);
        }
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (int col = 1; col <= lastCol; col++)
        {
            var name = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                map[name] = col;
        }
        return map;
    }

    private static string? GetCellString(IXLRow row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col)) return null;
        var value = row.Cell(col).GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static double GetCellDouble(IXLRow row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col)) return 0;
        return row.Cell(col).TryGetValue(out double val) ? val : 0;
    }

    private static int? GetCellInt(IXLRow row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col)) return null;
        return row.Cell(col).TryGetValue(out double val) ? (int)val : null;
    }

    private static bool GetCellBool(IXLRow row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col)) return false;
        var cell = row.Cell(col);
        if (cell.TryGetValue(out bool bVal)) return bVal;
        var str = cell.GetString().Trim().ToLowerInvariant();
        return str is "yes" or "true" or "1";
    }

    private static bool? GetCellBoolNullable(IXLRow row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col)) return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue(out bool bVal)) return bVal;
        var str = cell.GetString().Trim().ToLowerInvariant();
        return str is "yes" or "true" or "1";
    }

    private static List<string>? ParseCommaSeparatedList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static TransportType ParseTransportType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "car" or "car/van" => TransportType.Car,
            "public transport" or "public" => TransportType.PublicTransport,
            _ => TransportType.Either
        };
    }

    private static PermitDifficulty ParsePermitDifficulty(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "easy" => PermitDifficulty.Easy,
            "medium" => PermitDifficulty.Medium,
            "hard" => PermitDifficulty.Hard,
            _ => PermitDifficulty.Easy
        };
    }
}