using System.Globalization;
using System.Reflection;
using RouteOptimizer.Algorithm.Services;
using RouteOptimizer.Core.Models;
using RouteOptimizer.Data;
using RouteOptimizer.Data.Excel;

internal class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== PROGRAM STARTED ===");
        Console.WriteLine("BaseDir = " + AppContext.BaseDirectory);
        Console.WriteLine("Args = " + string.Join(" | ", args));

        try
        {
            // ---------------------------------------------------------
            // Формати запуску:
            // 1) 1 Excel (в ньому є і sites і technicians на листах)
            //    RouteOptimizer.Algorithm.exe input.xlsx output.xlsx 2026-02-16
            //
            // 2) 2 Excel (окремо)
            //    RouteOptimizer.Algorithm.exe sites.xlsx technicians.xlsx output.xlsx 2026-02-16
            // ---------------------------------------------------------

            string sitesPath;
            string techsPath;
            string outputPath;
            DateTimeOffset day;

            if (args.Length == 0)
            {
                sitesPath = "input.xlsx";
                techsPath = "input.xlsx";
                outputPath = "output_schedule.xlsx";
                day = StartOfWeek(DateTimeOffset.Now, DayOfWeek.Monday);

            }
            else if (args.Length == 3)
            {
                // 1 excel
                sitesPath = args[0];
                techsPath = args[0];
                outputPath = args[1];
                day = DateTimeOffset.Parse(args[2], CultureInfo.InvariantCulture).Date;
            }
            else if (args.Length >= 4)
            {
                // 2 excel
                sitesPath = args[0];
                techsPath = args[1];
                outputPath = args[2];
                day = DateTimeOffset.Parse(args[3], CultureInfo.InvariantCulture).Date;
            }
            else
            {
                PrintUsage();
                return 1;
            }

            // ---------------------------------------------------------
            // 0) Розв’язуємо шляхи відносно папки запуску
            // ---------------------------------------------------------
            var baseDir = AppContext.BaseDirectory;

            var fullSitesPath = ResolvePath(baseDir, sitesPath);
            var fullTechsPath = ResolvePath(baseDir, techsPath);
            var fullOutputPath = ResolvePath(baseDir, outputPath);

            Console.WriteLine($"BaseDir: {baseDir}");
            Console.WriteLine($"Sites:   {fullSitesPath}");
            Console.WriteLine($"Techs:   {fullTechsPath}");
            Console.WriteLine($"Output:  {fullOutputPath}");
            Console.WriteLine($"Day:     {day:yyyy-MM-dd}");
            Console.WriteLine();

            if (!File.Exists(fullSitesPath))
            {
                Console.WriteLine($"❌ Не знайдено файл sites: {fullSitesPath}");
                return 1;
            }
            if (!File.Exists(fullTechsPath))
            {
                Console.WriteLine($"❌ Не знайдено файл technicians: {fullTechsPath}");
                return 1;
            }

            // ---------------------------------------------------------
            // 1) Читаємо та процесимо дані
            // ---------------------------------------------------------
            var processor = new DataProcessor();
            ProcessedData processed;

            if (Path.GetFullPath(fullSitesPath) == Path.GetFullPath(fullTechsPath))
            {
                // 1 Excel — напряму
                using var stream = File.OpenRead(fullSitesPath);
                processed = processor.ProcessFromExcel(stream, day);
                Console.WriteLine("=== COORD CHECK ===");
                Console.WriteLine("Sites with 0,0 coords: " +
                    processed.Sites.Count(s => s.Coordinates == null || (s.Coordinates.Latitude == 0 && s.Coordinates.Longitude == 0)));

                Console.WriteLine("Techs with 0,0 home: " +
                    processed.Technicians.Count(t => t.HomeLocation == null || (t.HomeLocation.Latitude == 0 && t.HomeLocation.Longitude == 0)));

                Console.WriteLine("Example site coords: " +
                    processed.Sites.FirstOrDefault()?.Coordinates?.Latitude + ", " +
                    processed.Sites.FirstOrDefault()?.Coordinates?.Longitude);

                Console.WriteLine($"DayOfWeek = {day.DayOfWeek}");
                Console.WriteLine($"Technicians loaded: {processed.Technicians.Count}");

                foreach (var t in processed.Technicians.Take(10))
                {
                    Console.WriteLine($"--- {t.Id} {t.Name}");
                    Console.WriteLine($"WorkingDays: {(t.WorkingDays.Count == 0 ? "<EMPTY>" : string.Join(", ", t.WorkingDays))}");

                    if (t.DailySchedule == null || t.DailySchedule.Count == 0)
                    {
                        Console.WriteLine("DailySchedule: <EMPTY>");
                    }
                    else
                    {
                        Console.WriteLine("DailySchedule keys: " + string.Join(", ", t.DailySchedule.Keys));
                        if (t.DailySchedule.TryGetValue(day.DayOfWeek, out var sch))
                            Console.WriteLine($"Schedule for {day.DayOfWeek}: {sch.Start:hh\\:mm}-{sch.End:hh\\:mm}");
                        else
                            Console.WriteLine($"Schedule for {day.DayOfWeek}: <MISSING>");
                    }
                }

            }
            else
            {
                // 2 Excel — читаємо окремо через ExcelReader
                var reader = new ExcelReader();

                List<ServiceSite> sites;
                List<Technician> techs;

                using (var s = File.OpenRead(fullSitesPath))
                    sites = reader.ReadSites(s);

                using (var t = File.OpenRead(fullTechsPath))
                    techs = reader.ReadTechnicians(t);

                // DataProcessor.ProcessParsedData(...) private -> обходимо reflection-ом
                processed = InvokeProcessParsedData(processor, sites, techs, day);

            }

            Console.WriteLine($"Sites: {processed.Sites.Count}, Technicians: {processed.Technicians.Count}, Visits: {processed.Visits.Count}");
            Console.WriteLine($"Planning horizon (weeks): {processed.PlanningHorizonWeeks}");
            Console.WriteLine();

            // ---------------------------------------------------------
            // 2) Запуск оптимізації на 1 день
            // ---------------------------------------------------------
            var routing = new RoutingService(
                processed.Technicians,
                processed.Visits,
                processed.DistanceMatrix);

            var finalSchedule = new Schedule
            {
                StartDate = processed.StartDate,
                PlanningHorizonWeeks = processed.PlanningHorizonWeeks
            };

            var horizonDays = processed.PlanningHorizonWeeks * 7;
            // ✅ скільки хвилин вже "витратили" в поточному тижні для кожного техніка
            var weekUsedMinutes = processed.Technicians.ToDictionary(t => t.Id, _ => 0);

            // ✅ щоб знати, коли почався тиждень
            var weekStart = processed.StartDate.Date;
            var originalMaxHoursPerDay = processed.Technicians.ToDictionary(t => t.Id, t => t.MaxHoursPerDay);
            for (int d = 0; d < horizonDays; d++)
            {
                foreach (var tech in processed.Technicians)
                    tech.MaxHoursPerDay = originalMaxHoursPerDay[tech.Id];
                var currentDay = processed.StartDate.Date.AddDays(d);
                // якщо почався новий тиждень — обнуляємо лічильники
                if ((currentDay - weekStart).TotalDays >= 7)
                {
                    weekStart = currentDay;
                    foreach (var key in weekUsedMinutes.Keys.ToList())
                        weekUsedMinutes[key] = 0;
                }

                // тимчасово урізаємо MaxHoursPerDay, щоб не перевищити MaxHoursPerWeek
                foreach (var tech in processed.Technicians)
                {
                    int weekLimitMin = (tech.MaxHoursPerWeek > 0 ? tech.MaxHoursPerWeek * 60 : 0);
                    if (weekLimitMin == 0) continue;

                    int used = weekUsedMinutes[tech.Id];
                    int remaining = Math.Max(0, weekLimitMin - used);

                    // дозволені години сьогодні = min(звичні денні, залишок тижня)
                    int dayLimitMin = tech.MaxHoursPerDay * 60;
                    int effectiveTodayMin = Math.Min(dayLimitMin, remaining);

                    // якщо 0 — технік фактично "не працює" сьогодні
                    // (просто ставимо MaxHoursPerDay дуже маленьким)
                    tech.MaxHoursPerDay = (int)Math.Ceiling(effectiveTodayMin / 60.0);
                }

                var dayVisits = processed.Visits
                    .Where(v => v.ScheduledDate.Date == currentDay)
                    .ToList();

                var daySchedule = routing.SolveForDay(currentDay, dayVisits);
                foreach (var r in daySchedule.Routes)
                {
                    // рахуємо скільки хвилин зайняв маршрут (приблизно)
                    int minutes = r.TotalDurationMinutes;
                    weekUsedMinutes[r.TechnicianId] += Math.Max(0, minutes);
                }

                // додаємо всі маршрути
                finalSchedule.Routes.AddRange(daySchedule.Routes);

                // додаємо unassigned
                if (daySchedule.UnassignedVisitIds != null && daySchedule.UnassignedVisitIds.Count > 0)
                {
                    finalSchedule.UnassignedVisitIds ??= new List<string>();
                    finalSchedule.UnassignedVisitIds.AddRange(daySchedule.UnassignedVisitIds);
                }

                Console.WriteLine($"Day {currentDay:yyyy-MM-dd}: visits={dayVisits.Count}, assigned={daySchedule.Routes.Sum(r => r.Stops.Count)}, unassigned={daySchedule.UnassignedVisitIds.Count}");
            }

          


            // ---------------------------------------------------------
            // 3) Запис у Excel
            // ---------------------------------------------------------
            var writer = new ExcelWriter();

            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);

            using (var outStream = File.Create(fullOutputPath))
            {
                writer.WriteSchedule(finalSchedule, processed.Technicians, outStream);
            }

            Console.WriteLine($"✅ Готово! Schedule збережено в:\n{fullOutputPath}");
            Console.WriteLine("=== PROGRAM END ===");
            Console.ReadLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Помилка:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("=== PROGRAM END ===");
            Console.ReadLine();

            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Неправильні аргументи.");
        Console.WriteLine("1 Excel:  RouteOptimizer.Algorithm.exe input.xlsx output.xlsx 2026-02-16");
        Console.WriteLine("2 Excel:  RouteOptimizer.Algorithm.exe sites.xlsx techs.xlsx output.xlsx 2026-02-16");
    }

    private static string ResolvePath(string baseDir, string path)
    {
        // якщо абсолютний — лишаємо як є
        if (Path.IsPathRooted(path))
            return path;

        // якщо відносний — від baseDir (bin/Debug/net8.0)
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    private static ProcessedData InvokeProcessParsedData(
        DataProcessor processor,
        List<ServiceSite> sites,
        List<Technician> techs,
        DateTimeOffset startDate)
    {
        var method = typeof(DataProcessor).GetMethod(
            "ProcessParsedData",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (method == null)
            throw new MissingMethodException("DataProcessor", "ProcessParsedData (private) не знайдено. Перевір назву методу.");

        var result = method.Invoke(processor, new object[] { sites, techs, startDate });

        if (result is not ProcessedData processed)
            throw new InvalidOperationException("ProcessParsedData повернув не ProcessedData. Перевір сигнатуру методу.");

        return processed;
    }
    static DateTimeOffset StartOfWeek(DateTimeOffset date, DayOfWeek startDay = DayOfWeek.Monday)
    {
        int diff = (7 + (date.DayOfWeek - startDay)) % 7;
        return date.AddDays(-diff).Date;
    }
}
