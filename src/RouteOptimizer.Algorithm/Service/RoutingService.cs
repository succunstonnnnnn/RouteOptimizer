using Google.OrTools.ConstraintSolver;
using RouteOptimizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RouteOptimizer.Algorithm.Services
{
    public class RoutingService
    {
        private readonly List<Technician> _technicians;
        private readonly List<VisitInstance> _visits;
        private readonly DistanceMatrix _distanceMatrix;

        private const int BetweenVisitsBufferMinutes = 20;
        private const double AvgSpeedKmPerHour = 30.0;

        // Щоб OR-Tools міг "викидати" неможливі візити замість solution=null
        private const long UnassignedPenalty = 1_000_000;

        public RoutingService(
            List<Technician> technicians,
            List<VisitInstance> visits,
            DistanceMatrix distanceMatrix)
        {
            _technicians = technicians ?? throw new ArgumentNullException(nameof(technicians));
            _visits = visits ?? throw new ArgumentNullException(nameof(visits));
            _distanceMatrix = distanceMatrix ?? throw new ArgumentNullException(nameof(distanceMatrix));
        }

        public Schedule SolveForDay(DateTimeOffset day)
        {
            var dayVisits = _visits
                .Where(v => v.ScheduledDate.Date == day.Date)
                .ToList();

            return SolveForDay(day, dayVisits);
        }
        public Schedule SolveForDay(DateTimeOffset day, List<VisitInstance> dayVisits)
        {
            var schedule = new Schedule
            {
                StartDate = day,
                PlanningHorizonWeeks = 1
            };

            if (dayVisits.Count == 0)
            {
                Console.WriteLine($"⚠️ На дату {day:yyyy-MM-dd} немає VisitInstance (ScheduledDate не збігається).");
                schedule.UnassignedVisitIds = new List<string>();
                return schedule;
            }

            if (_technicians.Count == 0)
            {
                Console.WriteLine("⚠️ Немає техніків.");
                schedule.UnassignedVisitIds = dayVisits.Select(v => v.Id).ToList();
                return schedule;
            }

            // ⬇️⬇️⬇️ СЮДИ ВСТАВЛЯЄШ ВЕСЬ КОД, ЯКИЙ БУВ У ТВОЄМУ СТАРОМУ SolveForDay,
            // починаючи з перевірки missingSites і до return schedule;
            // (він у тебе вже є — просто вирізаєш і вставляєш сюди)

            // Перевірка: чи є всі service sites у матриці
            var missingSites = dayVisits
                .Select(v => v.ServiceSiteId)
                .Distinct()
                .Where(id => !HasLocation(id))
                .ToList();

            if (missingSites.Count > 0)
            {
                Console.WriteLine("❌ DistanceMatrix НЕ містить деякі ServiceSiteId. Приклади:");
                foreach (var m in missingSites.Take(10))
                    Console.WriteLine($"   - {m}");

                schedule.UnassignedVisitIds = dayVisits.Select(v => v.Id).ToList();
                return schedule;
            }

            int visitCount = dayVisits.Count;
            int vehicleCount = _technicians.Count;

            int totalNodes = visitCount + vehicleCount * 2;

            int[] starts = new int[vehicleCount];
            int[] ends = new int[vehicleCount];

            for (int v = 0; v < vehicleCount; v++)
            {
                starts[v] = visitCount + v * 2;
                ends[v] = visitCount + v * 2 + 1;
            }

            var manager = new RoutingIndexManager(totalNodes, vehicleCount, starts, ends);
            var routing = new RoutingModel(manager);

            var nodeToLocationId = BuildNodeToLocationMap(dayVisits);

            int travelCallback = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                int fromNode = manager.IndexToNode(fromIndex);
                int toNode = manager.IndexToNode(toIndex);

                string fromLoc = nodeToLocationId[fromNode];
                string toLoc = nodeToLocationId[toNode];

                double km = _distanceMatrix.GetDistance(fromLoc, toLoc);
                return KmToMinutes(km);
            });

            routing.SetArcCostEvaluatorOfAllVehicles(travelCallback);

            int timeCallback = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                int fromNode = manager.IndexToNode(fromIndex);
                int toNode = manager.IndexToNode(toIndex);

                string fromLoc = nodeToLocationId[fromNode];
                string toLoc = nodeToLocationId[toNode];

                double km = _distanceMatrix.GetDistance(fromLoc, toLoc);
                long travelMin = KmToMinutes(km);

                long serviceMin = 0;
                if (IsVisitNode(fromNode, visitCount))
                    serviceMin = dayVisits[fromNode].DurationMinutes;

                long buffer = (IsVisitNode(fromNode, visitCount) && IsVisitNode(toNode, visitCount))
                    ? BetweenVisitsBufferMinutes
                    : 0;

                return travelMin + serviceMin + buffer;
            });

            routing.AddDimension(
                timeCallback,
                24 * 60,
                24 * 60,
                false,
                "Time");

            var timeDimension = routing.GetDimensionOrDie("Time");

            for (int i = 0; i < visitCount; i++)
            {
                long idx = manager.NodeToIndex(i);
                var (startMin, endMin) = GetVisitWindowMinutesForDay(dayVisits[i], day);
                timeDimension.CumulVar(idx).SetRange(startMin, endMin);
            }

            for (int v = 0; v < vehicleCount; v++)
            {
                var tech = _technicians[v];
                var sched = tech.GetScheduleForDay(day.DayOfWeek);

                if (sched == null)
                    continue;

                long startMin = (long)sched.Value.Start.TotalMinutes;
                long endMin = (long)sched.Value.End.TotalMinutes;

                // ✅ MAX HOURS PER DAY
                long maxWorkMin = 0;
                if (tech.MaxHoursPerDay > 0)
                    maxWorkMin = tech.MaxHoursPerDay * 60L;

                long startIndex = routing.Start(v);
                long endIndex = routing.End(v);

                // старт фіксуємо
                timeDimension.CumulVar(startIndex).SetRange(startMin, startMin);

                // найпізніший можливий кінець: або кінець робочого дня, або start+maxHours
                long latestEnd = endMin;
                if (maxWorkMin > 0)
                    latestEnd = Math.Min(endMin, startMin + maxWorkMin);

                timeDimension.CumulVar(endIndex).SetRange(startMin, latestEnd);

                // додатково жорстка умова (на всякий)
                if (maxWorkMin > 0)
                    routing.solver().Add(timeDimension.CumulVar(endIndex) <= startMin + maxWorkMin);
            }

            for (int i = 0; i < visitCount; i++)
            {
                long nodeIndex = manager.NodeToIndex(i);
                routing.AddDisjunction(new long[] { nodeIndex }, UnassignedPenalty);
            }

            AddCompatibilityConstraints(routing, manager, dayVisits, visitCount, day);

            var search = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            search.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            search.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            search.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 5 };

            var solution = routing.SolveWithParameters(search);
            if (solution == null)
            {
                Console.WriteLine("❌ solution=null. Немає розв’язку навіть з disjunction (дивися constraints).");
                schedule.UnassignedVisitIds = dayVisits.Select(v => v.Id).ToList();
                return schedule;
            }

            BuildScheduleFromSolution(
                schedule,
                routing,
                manager,
                solution,
                timeDimension,
                nodeToLocationId,
                dayVisits,
                day,
                visitCount);

            schedule.UnassignedVisitIds = CollectUnassigned(routing, manager, solution, dayVisits, visitCount);

            int assignedStops = schedule.Routes.Sum(r => r.Stops.Count);
            Console.WriteLine($"✅ Assigned stops: {assignedStops} / {visitCount}. Unassigned: {schedule.UnassignedVisitIds.Count}");

            return schedule;
        }


        // ---------- mapping ----------

        private Dictionary<int, string> BuildNodeToLocationMap(List<VisitInstance> dayVisits)
        {
            var map = new Dictionary<int, string>();

            // visits
            for (int i = 0; i < dayVisits.Count; i++)
                map[i] = dayVisits[i].ServiceSiteId;

            // technician starts/ends exactly like DistanceMatrixBuilder
            for (int v = 0; v < _technicians.Count; v++)
            {
                var tech = _technicians[v];

                int startNode = dayVisits.Count + v * 2;
                int endNode = dayVisits.Count + v * 2 + 1;

                string startId = $"tech_{tech.Id}_start";
                string endId = $"tech_{tech.Id}_end";

                if (!HasLocation(startId))
                    throw new InvalidOperationException($"DistanceMatrix missing location: {startId}");

                // якщо end не додали (StartsFrom == FinishesAt) — end = start
                if (!HasLocation(endId))
                    endId = startId;

                map[startNode] = startId;
                map[endNode] = endId;
            }

            return map;
        }

        // ---------- constraints (з детальними причинами) ----------

        private void AddCompatibilityConstraints(
            RoutingModel routing,
            RoutingIndexManager manager,
            List<VisitInstance> dayVisits,
            int visitCount,
            DateTimeOffset day)
        {
            var solver = routing.solver();

            for (int i = 0; i < visitCount; i++)
            {
                long index = manager.NodeToIndex(i);
                var visit = dayVisits[i];

                int allowedCount = 0;
                var reasons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int v = 0; v < _technicians.Count; v++)
                {
                    var tech = _technicians[v];
                    bool allowed = true;
                    string reason = "OK";

                    // 1) працює цього дня + має schedule
                    if (!tech.CanWorkOn(day.DayOfWeek) || tech.GetScheduleForDay(day.DayOfWeek) == null)
                    {
                        allowed = false;
                        reason = "NOT_WORKING_TODAY";
                    }

                    // 2) skills + transport
                    // skills + preferred transport
                    if (allowed && visit.SkillsRequired != null)
                    {
                        var req = visit.SkillsRequired;

                        bool hasSkill = tech.Skills.ServiceSkills.Any(s =>
                            s.ServiceType == req.ServiceType &&
                            s.SkillLevel >= req.MinimumSkillLevel
                        );

                        if (!hasSkill)
                        {
                            // причина №1
                            allowed = false;
                            // debug
                            Console.WriteLine($"SKILLS_MISMATCH: visit={visit.Id} requires {req.ServiceType} >= {req.MinimumSkillLevel} | tech={tech.Id} has: " +
                                $"{string.Join(", ", tech.Skills.ServiceSkills.Select(s => $"{s.ServiceType}:{s.SkillLevel}"))}");
                        }

                        // додаткові прапорці (причина №2..N)
                        if (allowed && req.IsPhysicallyDemanding && !tech.Skills.CanDoPhysicallyDemanding) allowed = false;
                        if (allowed && req.RequiresLivingWalls && !tech.Skills.IsSkilledInLivingWalls) allowed = false;
                        if (allowed && req.RequiresHeightWork && !tech.Skills.IsComfortableWithHeights) allowed = false;
                        if (allowed && req.RequiresLift && !tech.Skills.HasLiftCertification) allowed = false;
                        if (allowed && req.RequiresPesticideCertification && !tech.Skills.HasPesticideCertification) allowed = false;
                        if (allowed && req.RequiresCitizenship && !tech.Skills.IsCitizen) allowed = false;

                        if (allowed && RequiresVehicle(req.PreferredTransport) && !tech.HasVehicle)
                            allowed = false;
                    }


                    // 3) whitelist
                    if (allowed && visit.AllowedTechnicianIds != null && visit.AllowedTechnicianIds.Count > 0
                        && !visit.AllowedTechnicianIds.Contains(tech.Id))
                    {
                        allowed = false;
                        reason = "NOT_IN_ALLOWED_LIST";
                    }

                    // 4) blacklist
                    if (allowed && visit.ForbiddenTechnicianIds != null && visit.ForbiddenTechnicianIds.Count > 0
                        && visit.ForbiddenTechnicianIds.Contains(tech.Id))
                    {
                        allowed = false;
                        reason = "IN_FORBIDDEN_LIST";
                    }

                    // 5) security list
                    if (allowed && visit.SecurityClearanceTechnicianIds != null && visit.SecurityClearanceTechnicianIds.Count > 0
                        && !visit.SecurityClearanceTechnicianIds.Contains(tech.Id))
                    {
                        allowed = false;
                        reason = "NO_CLEARANCE";
                    }

                    if (!allowed)
                    {
                        solver.Add(routing.VehicleVar(index) != v);
                        reasons[reason] = reasons.TryGetValue(reason, out var c) ? c + 1 : 1;
                    }
                    else
                    {
                        allowedCount++;
                    }
                }

                if (allowedCount == 0)
                {
                    Console.WriteLine($"⚠️ Visit {visit.Id} НЕ підходить жодному техніку. Причини:");
                    foreach (var kv in reasons.OrderByDescending(x => x.Value))
                        Console.WriteLine($"   - {kv.Key}: {kv.Value}");
                }
            }
        }

        private static bool RequiresVehicle(TransportType t) => t switch
        {
            TransportType.CarOrVan => true,
            TransportType.DriveToHubAndWalk => true, // теж потребує авто до хабу
            TransportType.Either => false,
            _ => false
        };

        // ---------- schedule build ----------

        private void BuildScheduleFromSolution(
            Schedule schedule,
            RoutingModel routing,
            RoutingIndexManager manager,
            Assignment solution,
            RoutingDimension timeDimension,
            Dictionary<int, string> nodeToLocationId,
            List<VisitInstance> dayVisits,
            DateTimeOffset day,
            int visitCount)
        {
            for (int v = 0; v < _technicians.Count; v++)
            {
                var tech = _technicians[v];

                var route = new Route
                {
                    Id = $"route-{tech.Id}-{day:yyyyMMdd}",
                    TechnicianId = tech.Id,
                    IsValid = true
                };

                long index = routing.Start(v);
                int seq = 1;

                string prevLocId = nodeToLocationId[manager.IndexToNode(index)];

                while (!routing.IsEnd(index))
                {
                    long nextIndex = solution.Value(routing.NextVar(index));
                    int node = manager.IndexToNode(nextIndex);

                    if (IsVisitNode(node, visitCount))
                    {
                        var visit = dayVisits[node];

                        int arrivalMin = (int)solution.Value(timeDimension.CumulVar(nextIndex));
                        var arrival = day.Date.AddMinutes(arrivalMin);
                        var departure = arrival.AddMinutes(visit.DurationMinutes);

                        string toLocId = nodeToLocationId[node];
                        double km = _distanceMatrix.GetDistance(prevLocId, toLocId);
                        int driveMin = (int)KmToMinutes(km);

                        route.Stops.Add(new RouteStop
                        {
                            Sequence = seq++,
                            VisitInstanceId = visit.Id,
                            ServiceSiteId = visit.ServiceSiteId,
                            ArrivalTime = arrival,
                            DepartureTime = departure,
                            DistanceFromPreviousKm = km,
                            DrivingTimeMinutes = driveMin,
                            IsWalkingFromPrevious = false
                        });

                        // позначимо visit
                        visit.AssignedTechnicianId = tech.Id;
                        visit.IsAssigned = true;
                        visit.RouteId = route.Id;

                        prevLocId = toLocId;
                    }

                    index = nextIndex;
                }

                route.TotalDistanceKm = route.Stops.Sum(s => s.DistanceFromPreviousKm);
                route.TotalDrivingMinutes = route.Stops.Sum(s => s.DrivingTimeMinutes);
                route.TotalDurationMinutes = route.Stops.Count == 0
                    ? 0
                    : (int)(route.Stops.Last().DepartureTime - route.Stops.First().ArrivalTime).TotalMinutes;

                schedule.Routes.Add(route);
            }
        }

        private List<string> CollectUnassigned(
            RoutingModel routing,
            RoutingIndexManager manager,
            Assignment solution,
            List<VisitInstance> dayVisits,
            int visitCount)
        {
            var unassigned = new List<string>();

            for (int i = 0; i < visitCount; i++)
            {
                long index = manager.NodeToIndex(i);
                long active = solution.Value(routing.ActiveVar(index)); // 1 = visited, 0 = dropped
                if (active == 0)
                    unassigned.Add(dayVisits[i].Id);
            }

            return unassigned;
        }

        // ---------- util ----------

        private bool HasLocation(string id)
            => _distanceMatrix.Locations.Any(l => l.Id == id);

        private static bool IsVisitNode(int nodeId, int visitCount)
            => nodeId >= 0 && nodeId < visitCount;

        private static long KmToMinutes(double km)
        {
            var minutes = (km / AvgSpeedKmPerHour) * 60.0;
            if (minutes < 0) minutes = 0;
            return (long)Math.Round(minutes);
        }

        private static (long StartMin, long EndMin) GetVisitWindowMinutesForDay(VisitInstance visit, DateTimeOffset day)
        {
            var tw = visit.TimeWindows.FirstOrDefault(x => x.DayOfWeek == day.DayOfWeek);
            if (tw == null)
                return (0, 24 * 60);

            long start = (long)tw.StartTime.TotalMinutes;
            long end = (long)tw.EndTime.TotalMinutes;

            if (end < start) end = 24 * 60;
            return (start, end);
        }
    }
}
