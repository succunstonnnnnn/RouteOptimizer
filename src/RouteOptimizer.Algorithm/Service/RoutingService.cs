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

        
        private const long UnassignedPenalty = 1_000_000;

        public RoutingService(
            List<Technician> technicians,
            List<VisitInstance> visits,
            DistanceMatrix distanceMatrix)
        {
            _technicians = (technicians ?? throw new ArgumentNullException(nameof(technicians)))
     .OrderBy(t => t.Id)
     .ToList();
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
            // ===== BREAK CONSTRAINTS (dummy visits) =====
            var breakVisits = new List<VisitInstance>();

            foreach (var tech in _technicians)
            {
                var br = tech.BreakRequirement;
                if (br == null) continue;

                int minBreak = br.MinBreakMinutes;
                if (minBreak <= 0) continue;

               
                var sched = tech.GetScheduleForDay(day.DayOfWeek);
                if (sched == null) continue;

                
                var startTs = br.BreakWindowStart != TimeSpan.Zero ? br.BreakWindowStart : sched.Value.Start;
                var endTs = br.BreakWindowEnd != TimeSpan.Zero ? br.BreakWindowEnd : sched.Value.End;

               
                if (endTs <= startTs)
                    endTs = sched.Value.End;

                
                if (startTs < sched.Value.Start) startTs = sched.Value.Start;
                if (endTs > sched.Value.End) endTs = sched.Value.End;

                
                if (endTs <= startTs) continue;

                var breakVisit = new VisitInstance
                {
                    Id = $"BREAK-{tech.Id}-{day:yyyyMMdd}",
                    ServiceId = "BREAK",
                    ServiceSiteId = $"tech_{tech.Id}_start",

                  
                    Latitude = tech.GetStartLocation().Latitude,
                    Longitude = tech.GetStartLocation().Longitude,

                    ScheduledDate = day,
                    DurationMinutes = minBreak,

                    TimeWindows = new List<TimeWindow>
        {
            new TimeWindow
            {
                DayOfWeek = day.DayOfWeek,
                StartTime = startTs,
                EndTime = endTs
            }
        },

                  
                    AllowedTechnicianIds = new List<string> { tech.Id },

                    SiteName = "BREAK",
                    SiteAddress = "BREAK"
                };

                breakVisits.Add(breakVisit);
            }

            
            if (breakVisits.Count > 0)
                dayVisits = dayVisits.Concat(breakVisits).ToList();
            
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
            timeDimension.SetGlobalSpanCostCoefficient(1);

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

                //  MAX HOURS PER DAY
                long maxWorkMin = 0;
                if (tech.MaxHoursPerDay > 0)
                    maxWorkMin = tech.MaxHoursPerDay * 60L;

                long startIndex = routing.Start(v);
                long endIndex = routing.End(v);

                
                timeDimension.CumulVar(startIndex).SetRange(startMin, startMin);

                
                long latestEnd = endMin;
                if (maxWorkMin > 0)
                    latestEnd = Math.Min(endMin, startMin + maxWorkMin);

                timeDimension.CumulVar(endIndex).SetRange(startMin, latestEnd);

              
                if (maxWorkMin > 0)
                    routing.solver().Add(timeDimension.CumulVar(endIndex) <= startMin + maxWorkMin);
            }

            for (int i = 0; i < visitCount; i++)
            {
              
                if (dayVisits[i].Id.StartsWith("BREAK-", StringComparison.OrdinalIgnoreCase))
                    continue;

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

               
                if (!HasLocation(endId))
                    endId = startId;

                map[startNode] = startId;
                map[endNode] = endId;
            }

            return map;
        }

        // ---------- constraints  ----------

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

                    
                    if (!tech.CanWorkOn(day.DayOfWeek) || tech.GetScheduleForDay(day.DayOfWeek) == null)
                    {
                        allowed = false;
                        reason = "NOT_WORKING_TODAY";
                    }

                   
                    if (allowed && visit.SkillsRequired != null)
                    {
                        var req = visit.SkillsRequired;

                        bool hasSkill = tech.Skills.ServiceSkills.Any(s =>
                            s.ServiceType == req.ServiceType &&
                            s.SkillLevel >= req.MinimumSkillLevel
                        );

                        if (!hasSkill)
                        {
                            allowed = false;
                            reason = "SKILLS_MISMATCH";
                        }
                        else if (req.IsPhysicallyDemanding && !tech.Skills.CanDoPhysicallyDemanding)
                        {
                            allowed = false;
                            reason = "PHYSICAL_MISMATCH";
                        }
                        else if (req.RequiresLivingWalls && !tech.Skills.IsSkilledInLivingWalls)
                        {
                            allowed = false;
                            reason = "LIVING_WALLS_REQUIRED";
                        }
                        else if (req.RequiresHeightWork && !tech.Skills.IsComfortableWithHeights)
                        {
                            allowed = false;
                            reason = "HEIGHT_WORK_REQUIRED";
                        }
                        else if (req.RequiresLift && !tech.Skills.HasLiftCertification)
                        {
                            allowed = false;
                            reason = "LIFT_CERT_REQUIRED";
                        }
                        else if (req.RequiresPesticideCertification && !tech.Skills.HasPesticideCertification)
                        {
                            allowed = false;
                            reason = "PESTICIDE_CERT_REQUIRED";
                        }
                        else if (req.RequiresCitizenship && !tech.Skills.IsCitizen)
                        {
                            allowed = false;
                            reason = "CITIZENSHIP_REQUIRED";
                        }
                        else if (RequiresVehicle(req.PreferredTransport) && !tech.HasVehicle)
                        {
                            allowed = false;
                            reason = "VEHICLE_REQUIRED";
                        }
                    }


                  
                    if (allowed && visit.AllowedTechnicianIds != null && visit.AllowedTechnicianIds.Count > 0
                        && !visit.AllowedTechnicianIds.Contains(tech.Id))
                    {
                        allowed = false;
                        reason = "NOT_IN_ALLOWED_LIST";
                    }

                    
                    if (allowed && visit.ForbiddenTechnicianIds != null && visit.ForbiddenTechnicianIds.Count > 0
                        && visit.ForbiddenTechnicianIds.Contains(tech.Id))
                    {
                        allowed = false;
                        reason = "IN_FORBIDDEN_LIST";
                    }

                   
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
            TransportType.DriveToHubAndWalk => true, 
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
                       
                        if (visit.Id.StartsWith("BREAK-", StringComparison.OrdinalIgnoreCase))
                        {
                            prevLocId = nodeToLocationId[node];
                            index = nextIndex;
                            continue;
                        }
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
                long active = solution.Value(routing.ActiveVar(index)); 
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
