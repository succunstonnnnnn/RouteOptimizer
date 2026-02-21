using ClosedXML.Excel;
using RouteOptimizer.Core.Models;

namespace RouteOptimizer.Data.Excel;

public class ExcelWriter
{
    public void WriteProcessedData(ProcessedData data, Stream outputStream)
    {
        using var workbook = new XLWorkbook();

        WriteSitesSheet(workbook, data.Sites);
        WriteTechniciansSheet(workbook, data.Technicians);
        WriteVisitsSheet(workbook, data.Visits);
        WriteDistanceMatrixSheet(workbook, data.DistanceMatrix);

        workbook.SaveAs(outputStream);
    }

    public void WriteSchedule(Schedule schedule, List<Technician> technicians, Stream outputStream)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Schedule");
        var wsSimple = workbook.AddWorksheet("Schedule_Simple");
        wsSimple.Cell(1, 1).Value = "Technician";
        wsSimple.Cell(1, 2).Value = "Day";
        wsSimple.Cell(1, 3).Value = "VisitIds";
        int simpleRow = 2;

        var headers = new[] { "Technician", "Day", "Visit Sequence", "Site Name",
            "Arrival Time", "Departure Time", "Service Duration (min)",
            "Travel Time (min)", "Distance (km)" };

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var techLookup = technicians.ToDictionary(t => t.Id, t => t.Name);
        int row = 2;

        foreach (var route in schedule.Routes.OrderBy(r => r.TechnicianId))
        {
            var techName = techLookup.GetValueOrDefault(route.TechnicianId, route.TechnicianId);

            foreach (var stop in route.Stops.OrderBy(s => s.Sequence))
            {
                ws.Cell(row, 1).Value = techName;
                ws.Cell(row, 2).Value = stop.ArrivalTime.ToString("yyyy-MM-dd (ddd)");
                ws.Cell(row, 3).Value = stop.Sequence;
                ws.Cell(row, 4).Value = stop.ServiceSiteId;
                ws.Cell(row, 5).Value = stop.ArrivalTime.ToString("HH:mm");
                ws.Cell(row, 6).Value = stop.DepartureTime.ToString("HH:mm");
                ws.Cell(row, 7).Value = (stop.DepartureTime - stop.ArrivalTime).TotalMinutes;
                ws.Cell(row, 8).Value = stop.DrivingTimeMinutes;
                ws.Cell(row, 9).Value = Math.Round(stop.DistanceFromPreviousKm, 2);
                row++;
            }
        }
        foreach (var route in schedule.Routes.OrderBy(r => r.TechnicianId))
        {
            if (route.Stops == null || route.Stops.Count == 0)
                continue;

            var techName = techLookup.GetValueOrDefault(route.TechnicianId, route.TechnicianId);

            
            var day = route.Stops.Min(s => s.ArrivalTime).ToString("yyyy-MM-dd");

            var visitIds = string.Join(", ",
                route.Stops
                    .OrderBy(s => s.Sequence)
                    .Select(s => s.VisitInstanceId)
            );

            wsSimple.Cell(simpleRow, 1).Value = techName;
            wsSimple.Cell(simpleRow, 2).Value = day;
            wsSimple.Cell(simpleRow, 3).Value = visitIds;
            simpleRow++;
        }

        wsSimple.Columns().AdjustToContents();
        ws.Columns().AdjustToContents();
        workbook.SaveAs(outputStream);
    }

    private static void WriteSitesSheet(XLWorkbook wb, List<ServiceSite> sites)
    {
        var ws = wb.AddWorksheet("Sites");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Address";
        ws.Cell(1, 4).Value = "Latitude";
        ws.Cell(1, 5).Value = "Longitude";
        ws.Cell(1, 6).Value = "ServiceCount";

        for (int i = 0; i < sites.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = sites[i].Id;
            ws.Cell(row, 2).Value = sites[i].Name ?? "";
            ws.Cell(row, 3).Value = sites[i].Address ?? "";
            ws.Cell(row, 4).Value = sites[i].Coordinates?.Latitude ?? 0;
            ws.Cell(row, 5).Value = sites[i].Coordinates?.Longitude ?? 0;
            ws.Cell(row, 6).Value = sites[i].Services?.Count ?? 0;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteTechniciansSheet(XLWorkbook wb, List<Technician> technicians)
    {
        var ws = wb.AddWorksheet("Technicians");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "HomeLat";
        ws.Cell(1, 4).Value = "HomeLon";
        ws.Cell(1, 5).Value = "StartsFrom";
        ws.Cell(1, 6).Value = "FinishesAt";
        ws.Cell(1, 7).Value = "WorkingDays";
        ws.Cell(1, 8).Value = "Skills";
        ws.Cell(1, 9).Value = "MaxHoursPerDay";
        ws.Cell(1, 10).Value = "MaxHoursPerWeek";

        for (int i = 0; i < technicians.Count; i++)
        {
            var row = i + 2;
            var tech = technicians[i];
            ws.Cell(row, 1).Value = tech.Id;
            ws.Cell(row, 2).Value = tech.Name;
            ws.Cell(row, 3).Value = tech.HomeLocation.Latitude;
            ws.Cell(row, 4).Value = tech.HomeLocation.Longitude;
            ws.Cell(row, 5).Value = tech.StartsFrom.ToString();
            ws.Cell(row, 6).Value = tech.FinishesAt.ToString();
            ws.Cell(row, 7).Value = string.Join(", ", tech.WorkingDays);
            ws.Cell(row, 8).Value = string.Join(", ", tech.Skills.ServiceSkills.Select(s => s.ToString()));
            ws.Cell(row, 9).Value = tech.MaxHoursPerDay;
            ws.Cell(row, 10).Value = tech.MaxHoursPerWeek;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteVisitsSheet(XLWorkbook wb, List<VisitInstance> visits)
    {
        var ws = wb.AddWorksheet("Visits");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "ServiceId";
        ws.Cell(1, 3).Value = "SiteId";
        ws.Cell(1, 4).Value = "SiteName";
        ws.Cell(1, 5).Value = "ScheduledDate";
        ws.Cell(1, 6).Value = "DurationMin";
        ws.Cell(1, 7).Value = "Latitude";
        ws.Cell(1, 8).Value = "Longitude";
        ws.Cell(1, 9).Value = "ServiceType";
        ws.Cell(1, 10).Value = "AssignedTech";

        for (int i = 0; i < visits.Count; i++)
        {
            var row = i + 2;
            var v = visits[i];
            ws.Cell(row, 1).Value = v.Id;
            ws.Cell(row, 2).Value = v.ServiceId;
            ws.Cell(row, 3).Value = v.ServiceSiteId;
            ws.Cell(row, 4).Value = v.SiteName;
            ws.Cell(row, 5).Value = v.ScheduledDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 6).Value = v.DurationMinutes;
            ws.Cell(row, 7).Value = v.Latitude;
            ws.Cell(row, 8).Value = v.Longitude;
            ws.Cell(row, 9).Value = v.SkillsRequired.ServiceType.ToString();
            ws.Cell(row, 10).Value = v.AssignedTechnicianId ?? "";
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteDistanceMatrixSheet(XLWorkbook wb, DistanceMatrix dm)
    {
        var ws = wb.AddWorksheet("DistanceMatrix");
        var n = dm.Locations.Count;

        for (int j = 0; j < n; j++)
            ws.Cell(1, j + 2).Value = dm.Locations[j].Id;

        for (int i = 0; i < n; i++)
        {
            ws.Cell(i + 2, 1).Value = dm.Locations[i].Id;
            for (int j = 0; j < n; j++)
                ws.Cell(i + 2, j + 2).Value = Math.Round(dm.Distances[i, j], 2);
        }

        ws.Columns().AdjustToContents();
    }
}
