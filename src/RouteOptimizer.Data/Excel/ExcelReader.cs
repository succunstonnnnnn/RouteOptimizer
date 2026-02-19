using ClosedXML.Excel;
using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Parsers;

namespace RouteOptimizer.Data.Excel;

public class ExcelReader
{
    public List<ServiceSite> ReadSites(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);

        // Auto-detect format: try "Service sites" (real format) first, then "Sites" (legacy)
        if (workbook.TryGetWorksheet("Service sites", out var realWs))
            return ReadSitesRealFormat(workbook, realWs);

        return ReadSitesLegacyFormat(workbook);
    }

    public List<Technician> ReadTechnicians(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("Technicians");

        // Detect format: if row 1 cell 1 is a group header (merged), it's the real format
        var firstCell = ws.Cell(1, 1).GetString().Trim();
        if (firstCell.Equals("Technician details", StringComparison.OrdinalIgnoreCase)
            || firstCell.Equals("Technician", StringComparison.OrdinalIgnoreCase)
            || !firstCell.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            // Check if this is really the new format (3 header rows, data at row 4)
            var row2Val = ws.Cell(2, 1).GetString().Trim();
            if (row2Val.Equals("Name", StringComparison.OrdinalIgnoreCase)
                || row2Val.Contains("name", StringComparison.OrdinalIgnoreCase))
                return ReadTechniciansRealFormat(ws);
        }

        return ReadTechniciansLegacyFormat(workbook, ws);
    }

    // ─── Real format: "Service sites" sheet with 3 header rows ───

    private List<ServiceSite> ReadSitesRealFormat(XLWorkbook workbook, IXLWorksheet ws)
    {
        var sites = new List<ServiceSite>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 3;
        int siteIndex = 0;

        for (int row = 4; row <= lastRow; row++)
        {
            var xlRow = ws.Row(row);
            var name = CellString(xlRow, 1);
            if (string.IsNullOrWhiteSpace(name)) continue;

            siteIndex++;
            var siteId = $"site-{siteIndex:D3}";
            var serviceId = $"svc-{siteIndex:D3}";

            var site = new ServiceSite
            {
                Id = siteId,
                Name = name,
                Address = CellString(xlRow, 2),
                Coordinates = new Coordinates(),
                BestAccessedBy = ParseTransportType(CellString(xlRow, 4)),
                RequiresPermit = CellBool(xlRow, 5),
                MustBeServicedWithSiteIds = ParseCommaSeparatedList(CellString(xlRow, 6)),
                PermitDifficulty = ParsePermitDifficulty(CellString(xlRow, 7)),
                TechsWithPermit = ParseCommaSeparatedList(CellString(xlRow, 8)),
                MondayStart = CellTimeString(xlRow, 9),
                MondayEnd = CellTimeString(xlRow, 10),
                TuesdayStart = CellTimeString(xlRow, 11),
                TuesdayEnd = CellTimeString(xlRow, 12),
                WednesdayStart = CellTimeString(xlRow, 13),
                WednesdayEnd = CellTimeString(xlRow, 14),
                ThursdayStart = CellTimeString(xlRow, 15),
                ThursdayEnd = CellTimeString(xlRow, 16),
                FridayStart = CellTimeString(xlRow, 17),
                FridayEnd = CellTimeString(xlRow, 18),
                SaturdayStart = CellTimeString(xlRow, 19),
                SaturdayEnd = CellTimeString(xlRow, 20),
                SundayStart = CellTimeString(xlRow, 21),
                SundayEnd = CellTimeString(xlRow, 22),
            };

            site.Availability = ServiceSiteParser.ParseAvailability(site);

            // Service data is embedded in the same row (cols 23-33)
            var skillStr = CellString(xlRow, 25);
            var jobType = ParseJobTypeFromSkillString(skillStr);

            var service = new Service
            {
                Id = serviceId,
                SiteId = siteId,
                JobType = jobType,
                FrequencyOfVisits = CellString(xlRow, 23),
                EstimatedDurationMinutes = CellInt(xlRow, 24) ?? 0,
                TechUserName = CellString(xlRow, 3),
                PhysicallyDemanding = CellBool(xlRow, 26),
                RequiresLivingWalls = CellBool(xlRow, 27),
                RequiresHeightWork = CellBool(xlRow, 28),
                RequiresLift = CellBool(xlRow, 29),
                RequiresPesticides = CellBool(xlRow, 30),
                RequiresCitizen = CellBool(xlRow, 31),
                AllowedTechnicianIds = ParseCommaSeparatedList(CellString(xlRow, 32)),
                ForbiddenTechnicianIds = ParseCommaSeparatedList(CellString(xlRow, 33)),
            };

            // Parse skill level from the combined string (e.g., "exterior - medior")
            var skillLevel = ParseSkillLevelFromSkillString(skillStr);
            if (skillLevel != null)
            {
                service.SkillsRequired = new SkillsRequired
                {
                    ServiceType = ServiceSiteParser.ParseServiceType(jobType),
                    MinimumSkillLevel = skillLevel.Value
                };
            }

            service.VisitFrequency = ServiceSiteParser.ParseFrequencyString(service.FrequencyOfVisits);
            if (service.EstimatedDurationMinutes <= 0)
                service.EstimatedDurationMinutes = DefaultDurationMinutes(jobType);

            site.Services = new List<Service> { service };
            sites.Add(site);
        }

        return sites;
    }

    private List<Technician> ReadTechniciansRealFormat(IXLWorksheet ws)
    {
        var technicians = new List<Technician>();

        // Знайдемо рядок, де є дні (Mon/Tue/...)
        int headerDaysRow = FindHeaderRowWithAny(ws, "Mon", "Monday", "Tue", "Tuesday", "Wed", "Wednesday");
        int headerFromToRow = headerDaysRow + 1;   // зазвичай наступний рядок містить from/to
        int firstDataRow = headerFromToRow + 1;    // дані після шапки

        var dayCols = BuildDayFromToColumnMap(ws, headerDaysRow, headerFromToRow);
        int skillsCol = FindColumnByHeader(ws, headerDaysRow, "Service skills", "ServiceSkills", "Skills");
        if (skillsCol <= 0)
            skillsCol = FindColumnByHeader(ws, headerFromToRow, "Service skills", "ServiceSkills", "Skills");

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstDataRow;
        int techIndex = 0;

        for (int row = firstDataRow; row <= lastRow; row++)
        {
            var xlRow = ws.Row(row);
            var name = CellString(xlRow, 1);
            if (string.IsNullOrWhiteSpace(name)) continue;

            techIndex++;
            var techId = $"tech-{techIndex:D3}";

            var dto = new TechnicianDto
            {
                Id = techId,
                Name = name!,
                HomeAddress = CellString(xlRow, 2),
                HomeLocation = new Coordinates(),
                StartsFrom = CellString(xlRow, 3),
                FinishesAt = CellString(xlRow, 4),

                MondayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Monday, isFrom: true),
                MondayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Monday, isFrom: false),

                TuesdayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Tuesday, true),
                TuesdayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Tuesday, false),

                WednesdayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Wednesday, true),
                WednesdayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Wednesday, false),

                ThursdayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Thursday, true),
                ThursdayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Thursday, false),

                FridayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Friday, true),
                FridayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Friday, false),

                SaturdayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Saturday, true),
                SaturdayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Saturday, false),

                SundayStart = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Sunday, true),
                SundayEnd = ReadTimeByDay(xlRow, dayCols, DayOfWeek.Sunday, false),

                // ⚠️ якщо ці колонки у твоєму Excel не 19..30 — тоді їх теж треба читати по шапці
                MinBreakMinutes = CellInt(xlRow, 19),
                BreakWindowStart = CellTimeString(xlRow, 20),
                BreakWindowEnd = CellTimeString(xlRow, 21),
                MaxHoursPerDay = CellInt(xlRow, 22),
                MaxHoursPerWeek = CellInt(xlRow, 23),
                ServiceSkills = CellString(xlRow, skillsCol) ?? CellString(xlRow, 24),

                CanDoPhysicallyDemanding = CellBoolNullable(xlRow, 25),
                IsSkilledInLivingWalls = CellBoolNullable(xlRow, 26),
                IsComfortableWithHeights = CellBoolNullable(xlRow, 27),
                HasLiftCertification = CellBoolNullable(xlRow, 28),
                HasPesticideCertification = CellBoolNullable(xlRow, 29),
                IsCitizen = CellBoolNullable(xlRow, 30),
            };

            var parser = new TechnicianParser();
            technicians.AddRange(parser.ParseFromJson(
                System.Text.Json.JsonSerializer.Serialize(new[] { dto })));
        }

        return technicians;
    }


    // ─── Legacy format: "Sites" sheet with 1 header row ───

    private List<ServiceSite> ReadSitesLegacyFormat(XLWorkbook workbook)
    {
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

    private List<Technician> ReadTechniciansLegacyFormat(XLWorkbook workbook, IXLWorksheet ws)
    {
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

    // ─── Positional cell helpers (for real format) ───

    private static string? CellString(IXLRow row, int col)
    {
        var value = row.Cell(col).GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? CellTimeString(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;

        // Try reading as DateTime first (Excel stores times as date-times with base date 1899-12-30)
        if (cell.TryGetValue(out DateTime dt))
            return dt.TimeOfDay.ToString(@"hh\:mm");

        // Fall back to string parsing
        var str = cell.GetString().Trim();
        if (string.IsNullOrEmpty(str)) return null;

        // Try parsing as TimeSpan directly
        if (TimeSpan.TryParse(str, out var ts))
            return ts.ToString(@"hh\:mm");

        return str;
    }

    private static int? CellInt(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        return cell.TryGetValue(out double val) ? (int)val : null;
    }

    private static bool CellBool(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return false;
        if (cell.TryGetValue(out bool bVal)) return bVal;
        var str = cell.GetString().Trim().ToLowerInvariant();
        return str is "yes" or "true" or "1";
    }

    private static bool? CellBoolNullable(IXLRow row, int col)
    {
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue(out bool bVal)) return bVal;
        var str = cell.GetString().Trim().ToLowerInvariant();
        return str is "yes" or "true" or "1";
    }

    // ─── Column-map based helpers (for legacy format) ───

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

    // ─── Shared parsing helpers ───

    private static List<string>? ParseCommaSeparatedList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static TransportType ParseTransportType(string? value)
    {
        var v = value?.Trim().ToLowerInvariant();

        return v switch
        {
            "car" or "car/van" or "car or van" or "van" => TransportType.CarOrVan,

            "public transport" or "public" or "bus" or "train"
                => TransportType.DriveToHubAndWalk, 

            "drive to a hub and then walk" or "drive to hub and walk" or "hub and walk"
                => TransportType.DriveToHubAndWalk,

            "either works" or "either" => TransportType.Either,
            null or "" => TransportType.Either,
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

    /// <summary>
    /// Extracts job type from a combined skill string like "exterior - medior" → "exterior"
    /// </summary>
    private static string? ParseJobTypeFromSkillString(string? skillStr)
    {
        if (string.IsNullOrWhiteSpace(skillStr)) return null;
        var parts = skillStr.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0].ToLowerInvariant() : null;
    }

    /// <summary>
    /// Extracts skill level from a combined skill string like "exterior - medior" → SkillLevel.Medior
    /// </summary>
    private static SkillLevel? ParseSkillLevelFromSkillString(string? skillStr)
    {
        if (string.IsNullOrWhiteSpace(skillStr)) return null;
        var parts = skillStr.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return null;
        return parts[1].ToLowerInvariant() switch
        {
            "junior" => SkillLevel.Junior,
            "medior" => SkillLevel.Medior,
            "senior" => SkillLevel.Senior,
            _ => null
        };
    }

    private static int DefaultDurationMinutes(string? jobType)
    {
        return jobType?.ToLowerInvariant() switch
        {
            "exterior" => 60,
            "interior" => 45,
            "floral" => 30,
            _ => 45
        };
    }
    private static int FindHeaderRowWithAny(IXLWorksheet ws, params string[] tokens)
    {
        // шукаємо у перших 30 рядках (надійніше)
        for (int r = 1; r <= 30; r++)
        {
            var row = ws.Row(r);
            var lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;

            for (int c = 1; c <= lastCol; c++)
            {
                var v = row.Cell(c).GetString().Trim();
                if (string.IsNullOrEmpty(v)) continue;

                if (tokens.Any(t => v.Equals(t, StringComparison.OrdinalIgnoreCase)))
                    return r;
            }
        }

        // fallback
        return 2;
    }

    private static Dictionary<DayOfWeek, (int FromCol, int ToCol)> BuildDayFromToColumnMap(
        IXLWorksheet ws, int dayRow, int fromToRow)
    {
        var map = new Dictionary<DayOfWeek, (int, int)>();

        var dayRowObj = ws.Row(dayRow);
        var fromToRowObj = ws.Row(fromToRow);

        var lastCol = Math.Max(
            dayRowObj.LastCellUsed()?.Address.ColumnNumber ?? 0,
            fromToRowObj.LastCellUsed()?.Address.ColumnNumber ?? 0);

        DayOfWeek? currentDay = null;

        for (int c = 1; c <= lastCol; c++)
        {
            var dayText = dayRowObj.Cell(c).GetString().Trim();
            if (!string.IsNullOrEmpty(dayText))
                currentDay = ParseDay(dayText);

            if (currentDay == null) continue;

            var ft = fromToRowObj.Cell(c).GetString().Trim().ToLowerInvariant();
            if (ft == "from")
            {
                int fromCol = c;
                int toCol = (c + 1 <= lastCol) ? c + 1 : c;
                map[currentDay.Value] = (fromCol, toCol);
            }
        }

        return map;
    }

    private static DayOfWeek? ParseDay(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        return t switch
        {
            "mon" or "monday" => DayOfWeek.Monday,
            "tue" or "tues" or "tuesday" => DayOfWeek.Tuesday,
            "wed" or "wednesday" => DayOfWeek.Wednesday,
            "thu" or "thursday" => DayOfWeek.Thursday,
            "fri" or "friday" => DayOfWeek.Friday,
            "sat" or "saturday" => DayOfWeek.Saturday,
            "sun" or "sunday" => DayOfWeek.Sunday,
            _ => null
        };
    }

    private static string? ReadTimeByDay(
        IXLRow row,
        Dictionary<DayOfWeek, (int FromCol, int ToCol)> map,
        DayOfWeek day,
        bool isFrom)
    {
        if (!map.TryGetValue(day, out var cols))
            return null;

        int col = isFrom ? cols.FromCol : cols.ToCol;
        return CellTimeString(row, col);
    }
    private static int FindColumnByHeader(IXLWorksheet ws, int headerRow, params string[] names)
    {
        var row = ws.Row(headerRow);
        var lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int c = 1; c <= lastCol; c++)
        {
            var text = row.Cell(c).GetString().Trim();
            if (string.IsNullOrEmpty(text)) continue;

            if (names.Any(n => text.Equals(n, StringComparison.OrdinalIgnoreCase)))
                return c;
        }

        return -1;
    }

}
