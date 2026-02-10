using RouteOptimizer.Core.Models;
namespace RouteOptimizer.Data.Preprocessing;

public class PlanningHorizonCalculator
{

    private const int DefaultWeeks = 4;
    private const int MaxWeeks = 12;

    public static int CalculateLCM(IEnumerable<int> frequencies)
    {
        var list = frequencies.ToList();
        if (list.Count == 0) return 4;

        return list.Aggregate(LCM);
    }

    private static int LCM(int a, int b)
    {
        return a / GCD(a, b) * b;
    }

    private static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    public static int CalculateFromServices(IEnumerable<Core.Models.Service> services)
    {
        var frequencies = services
            .Where(s => s.IsDeleted != true)
            .Select(s => (int)s.VisitFrequency)
            .Where(f => f > 0)
            .Distinct()
            .ToList();

        if (!frequencies.Any())
            return DefaultWeeks;

        var lcm = frequencies.Aggregate(LCM);
        return Math.Min(lcm, MaxWeeks);
    }
}
