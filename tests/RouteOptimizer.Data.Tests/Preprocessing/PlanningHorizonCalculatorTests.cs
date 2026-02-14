using RouteOptimizer.Core.Models;
using RouteOptimizer.Data.Preprocessing;

namespace RouteOptimizer.Data.Tests.Preprocessing;

public class PlanningHorizonCalculatorTests
{
    [Fact]
    public void CalculateLCM_EmptyList_ReturnsFour()
    {
        var result = PlanningHorizonCalculator.CalculateLCM(Array.Empty<int>());
        Assert.Equal(4, result);
    }

    [Fact]
    public void CalculateLCM_SingleValue_ReturnsThatValue()
    {
        Assert.Equal(2, PlanningHorizonCalculator.CalculateLCM(new[] { 2 }));
        Assert.Equal(3, PlanningHorizonCalculator.CalculateLCM(new[] { 3 }));
    }

    [Theory]
    [InlineData(new[] { 2, 3 }, 6)]
    [InlineData(new[] { 1, 2, 4 }, 4)]
    [InlineData(new[] { 2, 4 }, 4)]
    [InlineData(new[] { 1, 2, 3, 4 }, 12)]
    public void CalculateLCM_MultipleValues_ReturnsCorrectLCM(int[] frequencies, int expected)
    {
        Assert.Equal(expected, PlanningHorizonCalculator.CalculateLCM(frequencies));
    }

    [Fact]
    public void CalculateFromServices_NullIsDeleted_DoesNotCrash()
    {
        var services = new List<Service>
        {
            new() { VisitFrequency = VisitFrequency.BiWeekly, IsDeleted = null },
            new() { VisitFrequency = VisitFrequency.ThreeWeekly, IsDeleted = false }
        };

        var result = PlanningHorizonCalculator.CalculateFromServices(services);
        Assert.Equal(6, result); // LCM(2, 3) = 6
    }

    [Fact]
    public void CalculateFromServices_DeletedServicesExcluded()
    {
        var services = new List<Service>
        {
            new() { VisitFrequency = VisitFrequency.BiWeekly, IsDeleted = false },
            new() { VisitFrequency = VisitFrequency.FourWeeks, IsDeleted = true }
        };

        var result = PlanningHorizonCalculator.CalculateFromServices(services);
        Assert.Equal(2, result);
    }

    [Fact]
    public void CalculateFromServices_CappedAtMaxWeeks()
    {
        var services = new List<Service>
        {
            new() { VisitFrequency = VisitFrequency.ThreeWeekly, IsDeleted = false },
            new() { VisitFrequency = VisitFrequency.FourWeeks, IsDeleted = false }
        };

        var result = PlanningHorizonCalculator.CalculateFromServices(services);
        Assert.Equal(12, result); // LCM(3, 4) = 12
    }

    [Fact]
    public void CalculateFromServices_AllDeleted_ReturnsDefault()
    {
        var services = new List<Service>
        {
            new() { VisitFrequency = VisitFrequency.BiWeekly, IsDeleted = true }
        };

        var result = PlanningHorizonCalculator.CalculateFromServices(services);
        Assert.Equal(4, result);
    }
}